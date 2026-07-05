#if RIDER
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Rider.Model;
using JetBrains.Util;
using Sparkitect.RiderPlugin.Navigation;

namespace Sparkitect.RiderPlugin.Debug;

/// <summary>
/// The Solution-scoped host that wires the game-channel backend to the frontend tool window over the
/// generated <see cref="DebugToolWindowModel" /> Ext. This is the ONLY backend type that touches the
/// Rider Solution model, so it is the thin, Rider-only shim the rest of the debug backend keeps clear of
/// (<c>DebugChannelClient</c> / <c>DebugNavigation</c> / <c>DiscoveryWatcher</c> all compile against the
/// ReSharper SDK alone). It: (a) republishes the <see cref="DiscoveryWatcher" />'s live process list to
/// the selector (D-07); (b) on process selection, opens a <see cref="DebugChannelClient" /> and
/// republishes its cached snapshot to the window (D-06, Pitfall 5); and (c) answers row-navigation calls
/// via <see cref="DebugNavigation" /> (D-10). Everything is scoped to the solution lifetime; the
/// per-connection nested lifetime is recycled on every re-selection so a switch tears the old wire down.
/// </summary>
[SolutionComponent(Instantiation.ContainerAsyncPrimaryThread)]
public sealed class DebugToolWindowHost
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(DebugToolWindowHost));

    private readonly Lifetime myLifetime;
    private readonly ISolution mySolution;
    private readonly IShellLocks myLocks;
    private readonly DebugToolWindowModel myModel;
    private readonly DiscoveryWatcher myWatcher;

    // The current game-channel connection; terminated + replaced on each (re)selection.
    private LifetimeDefinition? myConnection;

    public DebugToolWindowHost(Lifetime lifetime, ISolution solution, IShellLocks locks)
    {
        myLifetime = lifetime;
        mySolution = solution;
        myLocks = locks;
        myModel = solution.GetProtocolSolution().GetDebugToolWindowModel();
        myWatcher = new DiscoveryWatcher(lifetime);

        // D-07: keep the selector's process list current. The watcher fires Changed on a background
        // thread; marshal the model write onto the protocol thread.
        myWatcher.Changed += OnDiscoveryChanged;
        PublishProcesses();

        // D-06/D-07: frontend selection drives which game channel the backend connects to.
        myModel.SelectedProcess.Advise(lifetime, Connect);

        // D-10: row navigation resolves through the shared reverse-lookup machinery.
        myModel.Navigate.Set((_, request) => Navigate(request));
    }

    private void OnDiscoveryChanged() =>
        myLocks.ExecuteOrQueueEx(myLifetime, "SparkitectDebug.Processes", PublishProcesses);

    private void PublishProcesses()
    {
        var processes = myWatcher.Processes
            .Select(p => new ProcessInfo(p.Pid, p.EngineVersion))
            .ToList();
        myModel.Processes.Value = processes;

        // D-07: auto-select a live process when the frontend has none, and drop (or replace) a selection
        // whose process is gone. Reading .Value of a never-set RdProperty throws, so probe through Maybe.
        var selected = myModel.SelectedProcess.Maybe;
        var current = selected.HasValue ? selected.Value : null;
        if (current != null && processes.All(p => p.Pid != current))
            myModel.SelectedProcess.Value = processes.Count > 0 ? processes[0].Pid : null;
        else if (current == null && processes.Count > 0)
            myModel.SelectedProcess.Value = processes[0].Pid;
    }

    private void Connect(int? pid)
    {
        myConnection?.Terminate();
        myConnection = null;

        if (pid == null)
        {
            myModel.Snapshot.Value = null;
            return;
        }

        var process = myWatcher.Processes.FirstOrDefault(p => p.Pid == pid.Value);
        if (process == null)
        {
            Logger.Warn($"SparkitectDebug: selected process {pid} is not in the discovered set; ignoring.");
            return;
        }

        var connection = myLifetime.CreateNested();
        myConnection = connection;

        // The client fires snapshots on its own scheduler thread; marshal the Ext republish onto the
        // protocol thread. The backend caches (holds the last value) simply by being the sole writer of
        // the Ext property — at a breakpoint pause the game pushes nothing new and the last value holds
        // (D-06). The raw snapshot (version marker included) is republished as-is so the frontend can
        // render the loud version-drift banner (D-09) for a mismatched marker.
        _ = new DebugChannelClient(connection.Lifetime, process.Port, snapshot =>
            myLocks.ExecuteOrQueueEx(connection.Lifetime, "SparkitectDebug.Snapshot",
                () => myModel.Snapshot.Value = snapshot));
    }

    private bool Navigate(NavigationRequest request)
    {
        var id = request.Id;
        Logger.Info($"SparkitectDebug: navigate ('{id.Mod}', '{id.Category}', '{id.Item}') target={request.Target}.");
        DocumentRange? range;
        try
        {
            range = ResolveTarget(id.Mod, id.Category, id.Item, request.Target);
        }
        catch (Exception e)
        {
            // An exception escaping the rd handler faults the call silently frontend-side; fail loud here.
            Logger.Error(e, $"SparkitectDebug: navigation failed for ('{id.Mod}', '{id.Category}', '{id.Item}').");
            return false;
        }
        if (range == null)
        {
            // D-11: no numeric fallback, no guessing — an unresolved row is surfaced loudly.
            Logger.Warn(
                $"SparkitectDebug: no {request.Target} target for ('{id.Mod}', '{id.Category}', '{id.Item}').");
            return false;
        }

        myLocks.ExecuteOrQueueEx(myLifetime, "SparkitectDebug.Navigate",
            () => NavigationSalvage.NavigateTo(mySolution, range.Value));
        return true;
    }

    /// <summary>
    /// Resolves a wire string triple to a source range under a read lock, trying each solution PSI module
    /// until the generated leaf resolves (the IDs structs live in the opened mod projects). Registration
    /// site for the context menu, type declaration for double-click (D-10).
    /// </summary>
    private DocumentRange? ResolveTarget(string mod, string category, string item, NavigationTarget target)
    {
        return myLocks.ExecuteWithReadLock(() =>
        {
            foreach (var module in mySolution.PsiModules().GetModules())
            {
                DocumentRange? range;
                try
                {
                    var leaf = DebugNavigation.ResolveLeaf(module, mod, category, item);
                    if (leaf == null)
                        continue;

                    range = target == NavigationTarget.RegistrationSite
                        ? DebugNavigation.ResolveRegistrationSite(leaf)
                        : DebugNavigation.ResolveTypeDeclaration(leaf);
                }
                catch (Exception e)
                {
                    // One exotic module must not abort the whole scan (an escaped exception here faults
                    // the rd call silently — the click just dies).
                    Logger.Warn($"SparkitectDebug: resolution threw in module '{module.DisplayName}': {e}");
                    continue;
                }

                Logger.Info(
                    $"SparkitectDebug: ('{mod}', '{category}', '{item}') resolved in module '{module.DisplayName}' → "
                    + (range == null ? "no range" : $"{range.Value.Document.Moniker} {range.Value.TextRange}"));
                return range;
            }

            return (DocumentRange?)null;
        });
    }
}
#endif
