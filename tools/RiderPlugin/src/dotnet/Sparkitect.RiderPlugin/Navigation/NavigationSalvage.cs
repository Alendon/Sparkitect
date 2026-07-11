using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Reusable resolution + navigation primitives for registration-id navigation: resolve a selected
/// node to its generated leaf id property (direct usage or the <c>X.Identification</c> auto-emit
/// forwarder), and navigate the editor caret to a document range via a <see cref="RangeOccurrence" />.
/// Carries no provider/Alt+Enter binding — those are layered on by the action that consumes it.
/// </summary>
public static class NavigationSalvage
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(NavigationSalvage));

    private const string IdentificationMemberName = "Identification";
    private const string HasIdentificationFullName = "Sparkitect.Modding.IHasIdentification";
    private const string OrderAfterAttributeFullName = "Sparkitect.Stateless.OrderAfterAttribute";
    private const string OrderBeforeAttributeFullName = "Sparkitect.Stateless.OrderBeforeAttribute";

    /// <summary>
    /// Cheap candidate gate for action update. Confirms the selected node could be a registration-id leaf
    /// without any uncached tree walk: a syntactic reference-expression check, the cached reference resolve,
    /// and metadata-level signals only. Direct leaf usages are confirmed exactly; the <c>X.Identification</c>
    /// auto-emit handle is admitted on its short name plus its containing type carrying a registration
    /// attribute, leaving the authoritative forwarder resolution to <see cref="ResolveLeafProperty" /> on the
    /// execute path (which no-ops when the handle does not actually forward to a leaf).
    /// </summary>
    public static bool IsLeafCandidate(ITreeNode? node)
    {
        var reference = node as IReferenceExpression ?? node?.Parent as IReferenceExpression;
        if (reference == null)
            return false;

        if (reference.Reference.Resolve().DeclaredElement is not IProperty property)
            return false;

        // Direct leaf usage: the property already lives on the generated IDs struct.
        if (RegistrationKey.FromLeafProperty(property) != null)
            return true;

        // Auto-emit handle: admit X.Identification on cheap structural/attribute signals. The containing
        // type is the registered type and carries the registration attribute; confirming the actual
        // forwarder target is deferred to the execute path.
        return property.ShortName == "Identification"
               && RegistrationMarkerPredicate.CarriesRegistrationAttribute(property.GetContainingType());
    }

    /// <summary>
    /// Resolves the selected node to the generated leaf id property. Handles both a direct usage of the
    /// leaf (<c>...ClearColor</c>) and the auto-emit handle (<c>X.Identification</c>), which forwards to
    /// the same leaf property. Authoritative and potentially expensive (the forwarder follows an uncached
    /// descendant walk); reserve it for the execute path, not per-poll action update.
    /// </summary>
    public static IProperty? ResolveLeafProperty(ITreeNode? node)
    {
        var reference = node as IReferenceExpression ?? node?.Parent as IReferenceExpression;
        if (reference == null)
            return null;

        if (reference.Reference.Resolve().DeclaredElement is not IProperty property)
            return null;

        // Direct leaf usage: the property already lives on the generated IDs struct.
        if (RegistrationKey.FromLeafProperty(property) != null)
            return property;

        // Auto-emit handle: X.Identification forwards to the generated leaf; follow the forwarder.
        return ResolveIdentificationForwarder(property);
    }

    /// <summary>Follows an <c>X.Identification</c> handle to the generated leaf id property it forwards to.</summary>
    public static IProperty? ResolveIdentificationForwarder(IProperty property)
    {
        if (property.ShortName != "Identification")
            return null;

        foreach (var declaration in property.GetDeclarations())
        foreach (var leaf in declaration.Descendants<IReferenceExpression>().Collect())
        {
            if (leaf.Reference.Resolve().DeclaredElement is IProperty target
                && RegistrationKey.FromLeafProperty(target) != null)
                return target;
        }

        return null;
    }

    /// <summary>
    /// Cheap candidate gate for the Order-Target action: the caret rests on a type argument of an
    /// <c>[OrderAfter&lt;T&gt;]</c>/<c>[OrderBefore&lt;T&gt;]</c> attribute. Purely syntactic (an enclosing type usage
    /// under an attribute whose short name is one of the ordering attributes); the authoritative attribute-type
    /// and wrapper resolution is deferred to <see cref="ResolveOrderTargetLeaf" /> on the execute path.
    /// </summary>
    public static bool IsOrderTargetCandidate(ITreeNode? node) => FindOrderTargetTypeUsage(node) != null;

    /// <summary>
    /// Resolves an ordering type-argument caret (<c>[OrderAfter&lt;X.SomeFunc&gt;]</c>) to the generated leaf id
    /// property the wrapper was emitted from: type argument -> the <c>{PascalId}Func</c> wrapper's
    /// <see cref="ITypeElement" /> -> its STATIC explicit-interface <c>IHasIdentification.Identification</c>
    /// member (never the co-named instance property, which forwards through <c>IdentificationHelper.Read&lt;T&gt;()</c>
    /// at runtime and is not PSI-walkable) -> the arrow-body reference expression -> the leaf
    /// <see cref="IProperty" />. The same wrapper shape serves StatelessFunctions and ECS systems (both
    /// implement <c>IHasIdentification</c> identically). Logs a warning on every unresolved step.
    /// </summary>
    public static IProperty? ResolveOrderTargetLeaf(ITreeNode? node)
    {
        var typeUsage = FindOrderTargetTypeUsage(node);
        if (typeUsage == null)
            return null;

        var attributeType =
            FindAncestor<IAttribute>(typeUsage)?.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!IsOrderingAttributeType(attributeType))
        {
            Logger.Warn("Order-target navigation: caret is not inside an OrderAfter/OrderBefore type argument.");
            return null;
        }

        var wrapperType = ResolveTypeUsageElement(typeUsage);
        if (wrapperType == null)
        {
            Logger.Warn($"Order-target navigation: type argument '{typeUsage.GetText()}' did not resolve to a type element.");
            return null;
        }

        var identification = ResolveStaticHasIdentificationMember(wrapperType);
        if (identification == null)
        {
            Logger.Warn($"Order-target navigation: '{wrapperType.GetClrName().FullName}' has no static IHasIdentification.Identification member.");
            return null;
        }

        var leaf = WalkToLeafProperty(identification);
        if (leaf == null)
            Logger.Warn($"Order-target navigation: the static Identification member on '{wrapperType.GetClrName().FullName}' did not reference a generated leaf id property.");

        return leaf;
    }

    /// <summary>The enclosing type-usage node when the caret sits on an ordering-attribute type argument, else null.</summary>
    private static ITypeUsage? FindOrderTargetTypeUsage(ITreeNode? node)
    {
        var typeUsage = FindAncestor<ITypeUsage>(node);
        if (typeUsage == null)
            return null;

        // Attribute names are not type usages, so any type usage inside an OrderAfter/OrderBefore attribute is
        // its generic argument; the authoritative attribute-type check is deferred to the execute path.
        var attribute = FindAncestor<IAttribute>(typeUsage);
        return IsOrderingAttributeShortName(attribute?.Name?.ShortName) ? typeUsage : null;
    }

    private static bool IsOrderingAttributeShortName(string? shortName) =>
        shortName is "OrderAfter" or "OrderAfterAttribute" or "OrderBefore" or "OrderBeforeAttribute";

    /// <summary>Authoritative check: the attribute type derives from the non-generic OrderAfter/OrderBefore base.</summary>
    private static bool IsOrderingAttributeType(ITypeElement? attributeType)
    {
        if (attributeType == null)
            return false;

        return MatchesOrderingBase(attributeType.GetClrName().FullName)
               || attributeType.GetAllSuperTypes().Any(t => MatchesOrderingBase(t.GetClrName().FullName));
    }

    private static bool MatchesOrderingBase(string? clrFullName) =>
        clrFullName == OrderAfterAttributeFullName || clrFullName == OrderBeforeAttributeFullName;

    /// <summary>Resolves a user type-usage node to its declared type element.</summary>
    private static ITypeElement? ResolveTypeUsageElement(ITypeUsage typeUsage) =>
        (typeUsage as IUserTypeUsage)?.ScalarTypeName?.Reference.Resolve().DeclaredElement as ITypeElement;

    /// <summary>
    /// The STATIC explicit-interface-implementation <c>IHasIdentification.Identification</c> member — the one
    /// whose arrow body is the literal leaf reference. Selected by member kind (static + explicit impl of
    /// <c>IHasIdentification</c>), never by name alone, so the co-named instance property is rejected.
    /// </summary>
    private static IProperty? ResolveStaticHasIdentificationMember(ITypeElement wrapperType)
    {
        foreach (var property in wrapperType.Properties)
        {
            if (!property.IsStatic || !property.IsExplicitImplementation
                                   || property.ShortName != IdentificationMemberName)
                continue;

            if (property.ExplicitImplementations.Any(
                    impl => impl.DeclaringType.GetClrName().FullName == HasIdentificationFullName))
                return property;
        }

        return null;
    }

    /// <summary>Walks a member's declaration body to the generated leaf id property its reference expression resolves to.</summary>
    private static IProperty? WalkToLeafProperty(IProperty member)
    {
        foreach (var declaration in member.GetDeclarations())
        foreach (var reference in declaration.Descendants<IReferenceExpression>().Collect())
        {
            if (reference.Reference.Resolve().DeclaredElement is IProperty leaf
                && RegistrationKey.FromLeafProperty(leaf) != null)
                return leaf;
        }

        return null;
    }

    private static T? FindAncestor<T>(ITreeNode? node) where T : class, ITreeNode
    {
        for (var current = node; current != null; current = current.Parent)
            if (current is T match)
                return match;

        return null;
    }

    /// <summary>Navigates the editor to a document range via a transient occurrence.</summary>
    public static void NavigateTo(ISolution solution, IDataContext dataContext, DocumentRange range)
    {
        var sourceFile = range.Document.GetPsiSourceFile(solution);
        if (sourceFile == null)
            return;

        var occurrence = new RangeOccurrence(sourceFile, range, false);
        var popupSource = NavigationOptions.FromDataContext(dataContext, "Navigate").PopupWindowContextSource;
        occurrence.Navigate(solution, popupSource, transferFocus: true);
    }

    /// <summary>
    /// Navigates the editor to a document range with no originating data context — the tool-window path,
    /// where the request comes from a tree row rather than an editor caret. Same transient-occurrence
    /// jump as the context overload, minus the popup-window anchoring (there is no source popup).
    /// </summary>
    public static void NavigateTo(ISolution solution, DocumentRange range)
    {
        var sourceFile = range.Document.GetPsiSourceFile(solution);
        if (sourceFile == null)
        {
            Logger.Warn($"Navigation: no PSI source file for document '{range.Document.Moniker}'; cannot navigate.");
            return;
        }

        var occurrence = new RangeOccurrence(sourceFile, range, false);
        if (occurrence.Navigate(solution, windowContext: null, transferFocus: true))
            Logger.Info($"Navigation: jumped to '{sourceFile.DisplayName}' at {range.TextRange}.");
        else
            Logger.Warn($"Navigation: jump to '{sourceFile.DisplayName}' at {range.TextRange} failed.");
    }
}
