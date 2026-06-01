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
    /// <summary>
    /// Resolves the selected node to the generated leaf id property. Handles both a direct usage of the
    /// leaf (<c>...ClearColor</c>) and the auto-emit handle (<c>X.Identification</c>), which forwards to
    /// the same leaf property.
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
}
