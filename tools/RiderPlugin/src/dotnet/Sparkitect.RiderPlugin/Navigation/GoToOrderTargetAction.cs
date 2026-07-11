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
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Go to Order Target: a Navigate-menu peer that resolves an <c>OrderAfter&lt;X.SomeFunc&gt;</c> /
/// <c>OrderBefore&lt;...&gt;</c> type argument to the authored method the generated <c>{PascalId}Func</c> wrapper
/// was emitted from. The wrapper already carries the full edge (its static explicit-interface
/// <c>IHasIdentification.Identification</c> member references the generated leaf id property, whose
/// <c>[RegisteredFrom]</c> owner is the authored method), so no source-generator change is needed — only this
/// plugin-side entry point. Serves StatelessFunctions and ECS systems alike (identical IHasIdentification
/// wrapper shape). Backend handler over the existing RD protocol, matched by id to the frontend action; no
/// rdgen model. Every unresolved branch logs loudly rather than no-op'ing silently.
/// </summary>
#pragma warning disable CS0612 // ActionAttribute(id, text) is the sole explicit-id ctor; SDK-obsolete but required to id-match the frontend action (mirrors GoToRegistrationAction).
[Action("GoToOrderTarget", "Order Target")]
#pragma warning restore CS0612
public sealed class GoToOrderTargetAction : IExecutableAction
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(GoToOrderTargetAction));

    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
        // Per-poll gate: purely syntactic (caret inside an OrderAfter/OrderBefore type-argument list). The
        // authoritative wrapper -> static Identification -> leaf resolution is deferred to Execute.
        var applicable = NavigationSalvage.IsOrderTargetCandidate(context.GetSelectedTreeNode<ITreeNode>());
        presentation.Visible = applicable;
        return applicable;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
        var solution = context.GetData(ProjectModelDataConstants.SOLUTION);
        if (solution == null)
        {
            Logger.Warn("Order-target navigation: no solution in the data context.");
            return;
        }

        var leaf = NavigationSalvage.ResolveOrderTargetLeaf(context.GetSelectedTreeNode<ITreeNode>());
        if (leaf == null)
            return; // ResolveOrderTargetLeaf already logged the specific unresolved step.

        var target = ResolveTarget(leaf, solution);
        if (target == null)
        {
            Logger.Warn("Order-target navigation: leaf resolved but no navigable registration or resource target found.");
            return;
        }

        NavigationSalvage.NavigateTo(solution, context, target.Value);
    }

    /// <summary>
    /// The single deterministic destination — the same target resolution Go to Registration uses: a C#
    /// registration anchors on its id-string literal (which, for a stateless function or ECS system, sits on
    /// the authored method), while a resource-file owner navigates to its <c>[RegisteredFrom]</c> coordinate.
    /// <see cref="RegistrationFactory.FromLeaf" /> is reused unchanged as the final hop.
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

        // The [RegisteredFrom] yaml coordinate is 1-based (YamlDotNet Mark); document lines and offsets are
        // 0-based, so shift both before resolving the entry-key offset.
        var lineIndex = owner.SourceLine > 0 ? owner.SourceLine - 1 : 0;
        if ((Int32<DocLine>)lineIndex >= document.GetLineCount())
            return null;

        var columnIndex = owner.SourceColumn > 0 ? owner.SourceColumn - 1 : 0;
        var offset = document.GetLineStartOffset((Int32<DocLine>)lineIndex) + columnIndex;
        return new DocumentRange(document, new TextRange(offset));
    }
}
