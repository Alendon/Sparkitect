using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;
using Sparkitect.RiderPlugin.References;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Go to Registration: a Navigate-menu peer to Go to Definition. From a generated ID-tree leaf usage
/// (<c>IDs.{Category}ID.{Mod}.{Entry}</c>) it lands on the authoritative registration identifier — the
/// C# registration-attribute id-string argument, or the resource-file entry coordinate. The owner edge is
/// the leaf's <c>[RegisteredFrom]</c> coordinate, read through the shared registration abstraction; the
/// destination is a single deterministic target, never a multi-target popup. Backend handler over the
/// existing RD protocol, matched by id to the frontend action; no rdgen model.
/// </summary>
[Action("GoToRegistration", "Registration")]
public sealed class GoToRegistrationAction : IExecutableAction
{
    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
        // Per-poll gate: stay cheap. Only a syntactic check, the cached reference resolve, and metadata
        // signals run here; the authoritative (and potentially expensive) leaf resolution is deferred to
        // Execute, which no-ops gracefully when no target resolves.
        var applicable = NavigationSalvage.IsLeafCandidate(context.GetSelectedTreeNode<ITreeNode>());
        presentation.Visible = applicable;
        return applicable;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
        var solution = context.GetData(ProjectModelDataConstants.SOLUTION);
        if (solution == null)
            return;

        var leaf = ResolveLeaf(context);
        if (leaf == null)
            return;

        var target = ResolveTarget(leaf, solution);
        if (target == null)
            return;

        NavigationSalvage.NavigateTo(solution, context, target.Value);
    }

    /// <summary>Gates on a caret resting on an ID-tree leaf usage (not on <c>T.Identification</c>).</summary>
    private static IProperty? ResolveLeaf(IDataContext context)
    {
        var node = context.GetSelectedTreeNode<ITreeNode>();
        return NavigationSalvage.ResolveLeafProperty(node);
    }

    /// <summary>
    /// The single deterministic destination. The registration subtype is resolved through the shared
    /// factory (the only category→subtype mapping); a C# registration anchors on its id-string literal,
    /// while a resource-file owner navigates to its <c>[RegisteredFrom]</c> path coordinate. The branch is
    /// on the owner's coordinate shape, never on registration category.
    /// </summary>
    private static DocumentRange? ResolveTarget(IProperty leaf, ISolution solution)
    {
        var registration = RegistrationFactory.FromLeaf(leaf);
        if (registration != null)
            return registration.NavigableTarget;

        var owner = RegisteredFromReader.Read(leaf);
        return string.IsNullOrEmpty(owner?.SourcePath)
            ? null
            : ResolveResourceRange(leaf, solution, owner!);
    }

    /// <summary>Resolves a resource-file owner coordinate (path relative to the project dir) to a range.</summary>
    private static DocumentRange? ResolveResourceRange(IProperty leaf, ISolution solution, RegistrationOwner owner)
    {
        var project = (leaf as IClrDeclaredElement)?.Module is IProjectPsiModule projectModule
            ? projectModule.Project
            : null;
        if (project == null)
            return null;

        var path = project.ProjectFileLocation.Directory.Combine(owner.SourcePath!);
        if (path.IsEmpty || !path.ExistsFile)
            return null;

        var sourceFile = solution.FindProjectItemsByLocation(path)
            .OfType<IProjectFile>()
            .FirstOrDefault()
            ?.ToSourceFile();
        var document = sourceFile?.Document;
        if (document == null)
            return null;

        // The [RegisteredFrom] yaml coordinate is 1-based (YamlDotNet Mark); document lines and
        // offsets are 0-based, so shift both before resolving the entry-key offset.
        var lineIndex = owner.SourceLine > 0 ? owner.SourceLine - 1 : 0;
        if ((Int32<DocLine>)lineIndex >= document.GetLineCount())
            return null;

        var columnIndex = owner.SourceColumn > 0 ? owner.SourceColumn - 1 : 0;
        var offset = document.GetLineStartOffset((Int32<DocLine>)lineIndex) + columnIndex;
        return new DocumentRange(document, new TextRange(offset));
    }
}
