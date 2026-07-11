#if RIDER
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Rider.Model;
using JetBrains.Util;
using Sparkitect.Debug.Protocol;
using Sparkitect.RiderPlugin.Debug;
using Sparkitect.RiderPlugin.Navigation;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.Explorer;

/// <summary>
/// The Solution-scoped host for the static Identification-structure explorer. It serves the per-mod
/// category→entry tree over the <see cref="ExplorerModel" /> Ext — a THIRD solution-scoped model, wholly
/// independent of the game channel: it NEVER constructs a <c>DebugChannelClient</c> or touches the game
/// <c>SocketWire</c>. The host is STATELESS: <c>Fetch</c> walks <see cref="ExplorerEnumeration" /> fresh
/// under a read lock on every call, storing nothing between calls. It (a) answers <c>Fetch</c> on demand;
/// (b) fires the payload-less <c>Invalidated</c> trigger on the 63-09-pinned broad compilation-change signal
/// (<see cref="ChangeManager" />.Changed), debounced onto the protocol thread via
/// <see cref="IShellLocks.ExecuteOrQueueEx" />; and (c) answers row navigation + the shader-source detail
/// supply through the shared <see cref="DebugNavigation" /> reverse lookup, verbatim.
/// </summary>
[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public sealed class ExplorerToolWindowHost
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(ExplorerToolWindowHost));

    // The one demo deep-inspector's category; the detail-supply below serves only this one.
    private const string ShaderModuleCategory = "shader_module";

    private readonly Lifetime myLifetime;
    private readonly ISolution mySolution;
    private readonly IShellLocks myLocks;
    private readonly ExplorerModel myModel;

    public ExplorerToolWindowHost(Lifetime lifetime, ISolution solution, IShellLocks locks, ChangeManager changeManager)
    {
        myLifetime = lifetime;
        mySolution = solution;
        myLocks = locks;
        myModel = solution.GetProtocolSolution().GetExplorerModel();

        // On-demand full-tree pull: freshly walked every call, nothing cached.
        myModel.Fetch.Set((_, _) => Fetch());

        // Row navigation resolves through the shared reverse-lookup machinery (reused verbatim).
        myModel.Navigate.Set((_, request) => Navigate(request));

        // The one demo deep-inspector: supply a shader-module entry's registration source text on demand.
        myModel.LoadShaderSource.Set((_, id) => LoadShaderSource(id));

        // Absorbed 63-09 axis (A): the broad resolved-compilation-change signal, strictly wider than PSI
        // document commits (project-model edits, SG output, persistent-index rebuilds all route through
        // it too). Fired on the UI thread; debounce onto the protocol thread so an edit burst cannot flood
        // Invalidated. The backend stores nothing -- this is a pure trigger, the frontend re-fetches.
        changeManager.Changed.Advise(lifetime, _ =>
            myLocks.ExecuteOrQueueEx(myLifetime, "SparkitectExplorer.Invalidate",
                () => myModel.Invalidated.Fire(Unit.Instance)));
    }

    /// <summary>
    /// Freshly walks every solution mod project and its generated Identification structure under a read
    /// lock, mapping <see cref="ExplorerEnumeration" />'s result onto the wire shape. Nothing is cached —
    /// called fresh on every <c>Fetch</c> request.
    /// </summary>
    private List<ModExplorerData> Fetch()
    {
        var mods = myLocks.ExecuteWithReadLock(() => ExplorerEnumeration.Enumerate(mySolution));
        return mods
            .Select(m => new ModExplorerData(
                new ModItem(m.ModId, m.DisplayName),
                m.Entries
                    .Select(e => new ExplorerEntry(e.Category, new IdName(m.ModId, e.Category, e.Item)))
                    .ToList()))
            .ToList();
    }

    private bool Navigate(NavigationRequest request)
    {
        var id = request.Id;
        Logger.Info($"SparkitectExplorer: navigate ('{id.Mod}', '{id.Category}', '{id.Item}') target={request.Target}.");
        DocumentRange? range;
        try
        {
            range = ResolveTarget(id.Mod, id.Category, id.Item, request.Target);
        }
        catch (Exception e)
        {
            // An exception escaping the rd handler faults the call silently frontend-side; fail loud here.
            Logger.Error(e, $"SparkitectExplorer: navigation failed for ('{id.Mod}', '{id.Category}', '{id.Item}').");
            return false;
        }
        if (range == null)
        {
            Logger.Warn(
                $"SparkitectExplorer: no {request.Target} target for ('{id.Mod}', '{id.Category}', '{id.Item}').");
            return false;
        }

        myLocks.ExecuteOrQueueEx(myLifetime, "SparkitectExplorer.Navigate",
            () => NavigationSalvage.NavigateTo(mySolution, range.Value));
        return true;
    }

    /// <summary>
    /// The one demo deep-inspector's detail supply (D-08): given a shader-module entry's id triple, returns
    /// the text of its registration source file, read-only. Strictly scoped to the shader-module category —
    /// every other category's detail viewer is SEEDED, not built. Fail-loud (<c>Logger.Warn</c>) and null on
    /// any unresolved step; an escaped exception faults the rd call silently, so it is caught and logged.
    /// </summary>
    private string? LoadShaderSource(IdName id)
    {
        if (id.Category != ShaderModuleCategory)
        {
            Logger.Warn($"SparkitectExplorer: shader-source supply asked for non-shader category '{id.Category}'.");
            return null;
        }

        try
        {
            return myLocks.ExecuteWithReadLock(() => ResolveShaderSource(id.Mod, id.Category, id.Item));
        }
        catch (Exception e)
        {
            Logger.Error(e, $"SparkitectExplorer: shader-source load failed for ('{id.Mod}', '{id.Category}', '{id.Item}').");
            return null;
        }
    }

    /// <summary>
    /// Resolves the shader-module leaf across solution PSI modules, reads its <c>[RegisteredFrom]</c> resource
    /// coordinate (shader modules are resource/file-backed), and returns that source file's text. Mirrors
    /// <c>GoToRegistrationAction.ResolveResourceRange</c>'s path resolution (path relative to the project dir),
    /// but yields the file content rather than a caret range. Read lock held by the caller.
    /// </summary>
    private string? ResolveShaderSource(string mod, string category, string item)
    {
        foreach (var module in mySolution.PsiModules().GetModules())
        {
            IProperty? leaf;
            try
            {
                leaf = DebugNavigation.ResolveLeaf(module, mod, category, item);
            }
            catch (Exception e)
            {
                Logger.Warn($"SparkitectExplorer: shader leaf resolution threw in module '{module.DisplayName}': {e}");
                continue;
            }
            if (leaf == null)
                continue;

            var owner = RegisteredFromReader.Read(leaf);
            if (string.IsNullOrEmpty(owner?.SourcePath))
            {
                Logger.Warn($"SparkitectExplorer: shader-module leaf '{item}' has no resource source coordinate.");
                return null;
            }

            var project = (leaf as IClrDeclaredElement)?.Module is IProjectPsiModule projectModule
                ? projectModule.Project
                : null;
            if (project == null)
            {
                Logger.Warn($"SparkitectExplorer: no owning project for shader-module leaf '{item}'.");
                return null;
            }

            var path = project.ProjectFileLocation.Directory.Combine(owner!.SourcePath!);
            if (path.IsEmpty || !path.ExistsFile)
            {
                Logger.Warn($"SparkitectExplorer: shader source file '{path}' not found for '{item}'.");
                return null;
            }

            var sourceFile = mySolution.FindProjectItemsByLocation(path)
                .OfType<IProjectFile>()
                .FirstOrDefault()
                ?.ToSourceFile();
            var text = sourceFile?.Document.GetText();
            if (text == null)
                Logger.Warn($"SparkitectExplorer: could not read shader source document '{path}' for '{item}'.");
            return text;
        }

        Logger.Warn($"SparkitectExplorer: no shader-module leaf resolved for ('{mod}', '{category}', '{item}').");
        return null;
    }

    /// <summary>
    /// Resolves a wire string triple to a source range under a read lock, trying each solution PSI module
    /// until the generated leaf resolves. Registration site for the context menu, type declaration for
    /// double-click. Identical routing to <c>DebugToolWindowHost.ResolveTarget</c> — the shared
    /// <see cref="DebugNavigation" /> path is category/registry-shape-agnostic.
    /// </summary>
    private DocumentRange? ResolveTarget(string mod, string category, string item, NavigationTarget target)
    {
        return myLocks.ExecuteWithReadLock(() =>
        {
            foreach (var module in mySolution.PsiModules().GetModules())
            {
                try
                {
                    var leaf = DebugNavigation.ResolveLeaf(module, mod, category, item);
                    if (leaf == null)
                        continue;

                    return target == NavigationTarget.RegistrationSite
                        ? DebugNavigation.ResolveRegistrationSite(leaf)
                        : DebugNavigation.ResolveTypeDeclaration(leaf);
                }
                catch (Exception e)
                {
                    // One exotic module must not abort the whole scan (an escaped exception faults the rd
                    // call silently — the click just dies).
                    Logger.Warn($"SparkitectExplorer: resolution threw in module '{module.DisplayName}': {e}");
                }
            }

            return (DocumentRange?)null;
        });
    }
}
#endif
