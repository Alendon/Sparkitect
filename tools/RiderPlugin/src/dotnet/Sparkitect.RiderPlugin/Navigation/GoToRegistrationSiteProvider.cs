using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Surfaces "Navigate from Here" / Alt+Enter targets on a usage of a generated registration id
/// (<c>{Category}ID.{Mod}.{Entry}</c>) or its auto-emit handle (<c>X.Identification</c>). The
/// registration site is offered FIRST (the default, one-keystroke target); the generated declaration
/// is offered second. Sites come from <see cref="RegistrationSiteIndex" />; if none are indexed only
/// the generated declaration is offered.
/// </summary>
[ContextNavigationProvider(Instantiation.DemandAnyThreadSafe)]
public sealed class GoToRegistrationSiteProvider : INavigateFromHereProvider
{
    public IEnumerable<ContextNavigation> CreateWorkflow(IDataContext dataContext)
    {
        var solution = dataContext.GetComponent<ISolution>();
        if (solution == null)
            yield break;

        var node = dataContext.GetSelectedTreeNode<ITreeNode>();
        var property = ResolveLeafProperty(node);
        if (property == null)
            yield break;

        var key = RegistrationKey.FromLeafProperty(property);
        if (key == null)
            yield break;

        var builder = solution.GetComponent<RegistrationSiteIndexBuilder>();
        var siteRanges = builder.GetOrBuild().TryGet(key.Value);

        foreach (var range in siteRanges)
        {
            var siteRange = range;
            yield return new ContextNavigation(
                "Registration site — " + DescribeRange(solution, siteRange),
                null,
                NavigationActionGroup.Other,
                () => NavigateTo(solution, dataContext, siteRange));
        }

        var declarationRange = GetGeneratedDeclarationRange(property);
        if (declarationRange.HasValue)
        {
            var genRange = declarationRange.Value;
            yield return new ContextNavigation(
                "Generated declaration — " + DescribeRange(solution, genRange),
                null,
                NavigationActionGroup.Other,
                () => NavigateTo(solution, dataContext, genRange));
        }
    }

    /// <summary>
    /// Resolves the selected node to the generated leaf id property. Handles both a direct usage of the
    /// leaf (<c>...ClearColor</c>) and the auto-emit handle (<c>X.Identification</c>), which forwards to
    /// the same leaf property.
    /// </summary>
    private static IProperty? ResolveLeafProperty(ITreeNode? node)
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

    private static IProperty? ResolveIdentificationForwarder(IProperty property)
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

    private static DocumentRange? GetGeneratedDeclarationRange(IProperty property)
    {
        var declaration = property.GetDeclarations().FirstOrDefault();
        if (declaration == null)
            return null;

        var range = declaration.GetNameDocumentRange();
        return range.IsValid() ? range : (DocumentRange?)null;
    }

    private static void NavigateTo(ISolution solution, IDataContext dataContext, DocumentRange range)
    {
        var sourceFile = range.Document.GetPsiSourceFile(solution);
        if (sourceFile == null)
            return;

        var occurrence = new RangeOccurrence(sourceFile, range, false);
        var popupSource = NavigationOptions.FromDataContext(dataContext, "Navigate").PopupWindowContextSource;
        occurrence.Navigate(solution, popupSource, transferFocus: true);
    }

    private static string DescribeRange(ISolution solution, DocumentRange range)
    {
        var document = range.Document;
        var fileName = document.GetPsiSourceFile(solution)?.Name ?? "source";
        var line = (int)range.StartOffset.ToDocumentCoords().Line + 1;
        return fileName + ":" + line;
    }
}
