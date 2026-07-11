using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.TextControl;
using JetBrains.TextControl.CodeWithMe;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.CodeInsights;

/// <summary>
/// Reverse ordering hint (RIDR-01): a code-vision "N order targets" lens on an authored
/// StatelessFunction / ECS-system method that counts the <c>[OrderAfter&lt;MyFunc&gt;]</c> /
/// <c>[OrderBefore&lt;MyFunc&gt;]</c> sites scheduling around it, and on click surfaces the scheduling
/// methods. The forward jump (arg -> authored method) lives in <see cref="Navigation.GoToOrderTargetAction"/>;
/// this is its mirror. The plugin's first <see cref="ICodeInsightsProvider"/>: a container-discovered
/// component ([SolutionComponent], no plugin.xml), driven by <see cref="OrderTargetsCodeVisionAnalyzer"/>.
/// Static, always available, no source-generator change.
/// </summary>
[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public sealed class OrderTargetsCodeVisionProvider : ICodeInsightsProvider
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(OrderTargetsCodeVisionProvider));

    public string ProviderId => "Sparkitect Order Targets";
    public string DisplayName => "Sparkitect order targets";
    public CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;

    // Lead the code-vision lenses on the declaration, matching the "N usages" leading position.
    public ICollection<CodeVisionRelativeOrdering> RelativeOrderings =>
        new CodeVisionRelativeOrdering[] { new CodeVisionRelativeOrderingFirst() };

    public bool IsAvailableIn(ISolution solution) => true;

    /// <summary>Pushes the "N order targets" lens onto the authored method declaration.</summary>
    public void AddHighlighting(
        IHighlightingConsumer consumer,
        IDeclaration declaration,
        IDeclaredElement declaredElement,
        int count)
    {
        var text = count == 1 ? "1 order target" : $"{count} order targets";
        const string tooltip = "Functions and systems scheduled around this one via OrderAfter/OrderBefore";
        consumer.AddHighlighting(new CodeInsightsHighlighting(
            declaration.GetNameDocumentRange(), text, tooltip, string.Empty, this, declaredElement, null!));
    }

    /// <summary>Click surfaces the scheduling methods that order around this one (Find-Usages-style candidate popup).</summary>
    public void OnClick(CodeInsightHighlightInfo highlightInfo, ISolution solution, CodeInsightsClickInfo clickInfo)
    {
        List<IDeclaredElement> owners;
        using (ReadLockCookie.Create())
        {
            if (highlightInfo.CodeInsightsHighlighting.DeclaredElement is not IMethod capturedMethod)
            {
                Logger.Verbose("Order-targets code-vision: click carried no method declared element.");
                return;
            }

            // The captured declared element can be stale by click time (PSI re-parse between the
            // highlight pass and the click); re-resolve fresh from its declarations rather than
            // trusting the cached reference.
            var method = capturedMethod.GetDeclarations()
                .OfType<IMethodDeclaration>()
                .FirstOrDefault()
                ?.DeclaredElement as IMethod;
            if (method == null || !method.IsValid())
            {
                Logger.Verbose("Order-targets code-vision: click method no longer resolves to a valid declaration.");
                return;
            }

            var wrapper = OrderTargetsResolution.ResolveWrapperType(method);
            if (wrapper == null)
            {
                Logger.Verbose("Order-targets code-vision: click method no longer owns a generated Func wrapper.");
                return;
            }

            owners = OrderTargetsResolution.FindSchedulingOwners(wrapper, solution, method.GetPsiServices());
        }

        if (owners.Count == 0)
        {
            Logger.Verbose("Order-targets code-vision: click resolved no ordering sites.");
            return;
        }

        // A code-vision click always originates from the focused editor; anchor the candidate popup to its caret.
        var textControl = solution.GetComponent<ITextControlManager>().FocusedTextControlPerClient.ForCurrentClient();
        if (textControl == null)
        {
            Logger.Verbose("Order-targets code-vision: no focused text control to anchor the candidate popup.");
            return;
        }

        var popupSource = textControl.PopupWindowContextFactory.ForCaret();
        solution.GetComponent<DeclaredElementNavigationService>().ExecuteCandidates(owners, popupSource, false);
    }

    public void OnExtraActionClick(CodeInsightHighlightInfo highlightInfo, string actionId, ISolution solution)
    {
    }
}

/// <summary>
/// Daemon driver for <see cref="OrderTargetsCodeVisionProvider"/>. Gates each method declaration to those
/// owning a generated <c>{PascalId}Func</c> wrapper before running any solution-wide usage search (T-63-03),
/// then contributes the lens only when at least one ordering site references that wrapper.
/// </summary>
[ElementProblemAnalyzer(
    Instantiation.DemandAnyThreadSafe,
    typeof(IMethodDeclaration),
    HighlightingTypes = new[] { typeof(CodeInsightsHighlighting) })]
public sealed class OrderTargetsCodeVisionAnalyzer : ElementProblemAnalyzer<IMethodDeclaration>
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(OrderTargetsCodeVisionAnalyzer));

    private readonly OrderTargetsCodeVisionProvider myProvider;

    public OrderTargetsCodeVisionAnalyzer(OrderTargetsCodeVisionProvider provider) => myProvider = provider;

    protected override void Run(
        IMethodDeclaration element,
        ElementProblemAnalyzerData data,
        IHighlightingConsumer consumer)
    {
        var method = element.DeclaredElement;
        if (method == null)
            return;

        // Gate before any solution-wide search: only wrapper-owning SF/ECS methods reach the finder.
        var wrapper = OrderTargetsResolution.ResolveWrapperType(method);
        if (wrapper == null)
            return;

        var owners = OrderTargetsResolution.FindSchedulingOwners(wrapper, data.Solution, element.GetPsiServices());
        if (owners.Count == 0)
        {
            Logger.Verbose($"Order-targets code-vision: '{method.ShortName}' owns a wrapper but has no ordering sites.");
            return;
        }

        myProvider.AddHighlighting(consumer, element, method, owners.Count);
    }
}

/// <summary>
/// Shared method -> wrapper -> ordering-site resolution. The method-to-wrapper hop uses the generator's
/// deterministic naming rule (<c>{ToPascalCase(identifier)}Func</c> nested in the containing type, per
/// <see cref="References.RegistrationKey.SnakeToPascal"/>), never a name-sniff; the reverse edge is a
/// usage search on the wrapper type filtered to OrderAfter/OrderBefore attribute arguments.
/// </summary>
internal static class OrderTargetsResolution
{
    private const string StatelessFunctionAttributeFullName = "Sparkitect.Stateless.StatelessFunctionAttribute";
    private const string OrderAfterAttributeFullName = "Sparkitect.Stateless.OrderAfterAttribute";
    private const string OrderBeforeAttributeFullName = "Sparkitect.Stateless.OrderBeforeAttribute";
    private const string WrapperSuffix = "Func";

    /// <summary>
    /// The sibling generated <c>{PascalId}Func</c> wrapper for an authored SF/ECS-system method, or null when
    /// the method carries no StatelessFunction-derived attribute or the wrapper is absent (unrelated method).
    /// </summary>
    public static ITypeElement? ResolveWrapperType(IMethod method)
    {
        var identifier = ReadStatelessIdentifier(method);
        if (string.IsNullOrEmpty(identifier))
            return null;

        var containingType = method.GetContainingType();
        if (containingType == null)
            return null;

        var wrapperShortName = RegistrationKey.SnakeToPascal(identifier!) + WrapperSuffix;
        return containingType.NestedTypes.FirstOrDefault(t => t.ShortName == wrapperShortName);
    }

    /// <summary>
    /// Distinct declaring methods/systems whose <c>[OrderAfter&lt;wrapper&gt;]</c> / <c>[OrderBefore&lt;wrapper&gt;]</c>
    /// attribute references the wrapper — the "who is scheduled around me" set. Solution-wide usage search on the
    /// wrapper type, filtered to ordering-attribute arguments (drops the generated Register/scheduling references).
    /// </summary>
    public static List<IDeclaredElement> FindSchedulingOwners(
        ITypeElement wrapper,
        ISolution solution,
        IPsiServices services)
    {
        var searchDomain = SearchDomainFactory.Instance.CreateSearchDomain(solution, false);
        var references = services.Finder.FindReferences(wrapper, searchDomain, NullProgressIndicator.Create());

        var owners = new HashSet<IDeclaredElement>();
        foreach (var reference in references)
        {
            var owner = ResolveOrderingOwner(reference.GetTreeNode());
            if (owner != null)
                owners.Add(owner);
        }

        return owners.ToList();
    }

    /// <summary>
    /// The identifier string of the method's StatelessFunction-derived attribute (SF, ECS, per-frame,
    /// transition), read straight from the attribute's PSI source literal (mirroring
    /// <see cref="Explorer.ExplorerEnumeration.CategoryViaRegisterMethod" />) rather than the constant
    /// evaluator, whose <c>PositionParameter</c> path NREs on net10.0 modules (RESEARCH Pitfall 4).
    /// </summary>
    private static string? ReadStatelessIdentifier(IMethod method)
    {
        foreach (var declaration in method.GetDeclarations())
        {
            if (declaration is not IAttributesOwnerDeclaration attributesOwner)
                continue;

            foreach (var attribute in attributesOwner.AttributesEnumerable)
            {
                var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
                if (attributeType == null || !InheritsStatelessFunction(attributeType))
                    continue;

                try
                {
                    var literal = attribute.ConstructorArgumentExpressions
                        .OfType<ICSharpLiteralExpression>()
                        .FirstOrDefault(l => l.IsConstantValue() && l.ConstantValue.IsString());
                    if (literal != null)
                        return literal.ConstantValue.AsString();
                }
                catch (System.NullReferenceException)
                {
                    // net10.0 constant-evaluator NRE backstop (RESEARCH Pitfall 4): degrade to absent.
                    return null;
                }
            }
        }

        return null;
    }

    private static bool InheritsStatelessFunction(ITypeElement attributeType) =>
        attributeType.GetClrName().FullName == StatelessFunctionAttributeFullName
        || attributeType.GetAllSuperTypes().Any(t => t.GetClrName().FullName == StatelessFunctionAttributeFullName);

    /// <summary>The declaring method/system a wrapper reference belongs to, but only when that reference is the type argument of an OrderAfter/OrderBefore attribute.</summary>
    private static IDeclaredElement? ResolveOrderingOwner(ITreeNode? node)
    {
        var attribute = FindAncestor<IAttribute>(node);
        if (attribute == null)
            return null;

        var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!IsOrderingAttributeType(attributeType))
            return null;

        return FindAncestor<IDeclaration>(attribute)?.DeclaredElement;
    }

    private static bool IsOrderingAttributeType(ITypeElement? attributeType)
    {
        if (attributeType == null)
            return false;

        return MatchesOrderingBase(attributeType.GetClrName().FullName)
               || attributeType.GetAllSuperTypes().Any(t => MatchesOrderingBase(t.GetClrName().FullName));
    }

    private static bool MatchesOrderingBase(string? clrFullName) =>
        clrFullName == OrderAfterAttributeFullName || clrFullName == OrderBeforeAttributeFullName;

    private static T? FindAncestor<T>(ITreeNode? node) where T : class, ITreeNode
    {
        for (var current = node; current != null; current = current.Parent)
            if (current is T match)
                return match;

        return null;
    }
}
