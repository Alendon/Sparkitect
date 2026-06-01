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
