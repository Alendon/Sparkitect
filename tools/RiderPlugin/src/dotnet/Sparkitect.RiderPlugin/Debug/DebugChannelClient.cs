using System;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using JetBrains.Threading;
using JetBrains.Util;
using Sparkitect.Debug.Protocol;
using Sparkitect.Debug.Protocol.Game;

namespace Sparkitect.RiderPlugin.Debug;

/// <summary>
/// The plugin backend's single rd client to a running game's debug channel. The frontend cannot socket
/// the game (Pitfall 5), so the backend is the sole game-channel client: it opens a
/// <see cref="SocketWire.Client" /> to the discovered port, binds the generated game-channel model, and
/// forwards every published snapshot (including the first on-connect one, D-06) to the supplied sink.
/// The republish over the Solution-scoped Ext is deliberately NOT done here — see
/// <c>DebugToolWindowHost</c>; keeping the Ext (which needs the Rider Solution model) out of this type
/// means the whole client — wire, protocol, model bind, snapshot cache — depends only on
/// <c>JetBrains.RdFramework</c> and the generated game channel, and so is covered by the plain backend
/// compile oracle.
/// </summary>
/// <remarks>
/// Everything is scoped to the supplied <see cref="Lifetime" />: terminating it disposes the scheduler
/// thread, wire, protocol, and model, so a process switch / window close / solution close tears the
/// connection down cleanly. Connect and model-bind failures are logged loudly (fail-loud doctrine) and
/// never silently swallowed — a refused port surfaces in the backend log rather than a dead-quiet window.
/// </remarks>
internal sealed class DebugChannelClient
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(DebugChannelClient));

    /// <summary>
    /// Opens a client to <paramref name="port" /> on loopback and advises the game-channel snapshot into
    /// <paramref name="onSnapshot" /> (the value is null until the engine's first publish). All rd objects
    /// live on <paramref name="lifetime" />; a dedicated single-thread scheduler (torn down with the
    /// lifetime) marshals every rd callback. <paramref name="onSnapshot" /> is invoked on that scheduler
    /// thread — the caller marshals to the protocol thread before touching the Solution Ext.
    /// </summary>
    public DebugChannelClient(Lifetime lifetime, int port, Action<DebugSnapshot?> onSnapshot)
    {
        if (onSnapshot == null) throw new ArgumentNullException(nameof(onSnapshot));

        try
        {
            // rd requires a single, stable message-pump thread per protocol; one per connection,
            // terminated with the lifetime.
            var scheduler = SingleThreadScheduler.RunOnSeparateThread(lifetime, "SparkitectDebugChannelClient");
            var wire = new SocketWire.Client(lifetime, scheduler, port, "SparkitectDebugChannelClient");
            var protocol = new Protocol("SparkitectDebug", new Serializers(),
                new Identities(IdKind.Client), scheduler, wire, lifetime);

            // Model construction binds the toplevel and must run on the protocol scheduler (rd threading
            // contract); the advise callback then fires on the same thread.
            scheduler.Queue(() =>
            {
                try
                {
                    var model = new SparkitectDebugModel(lifetime, protocol);
                    model.Snapshot.Advise(lifetime, snapshot => onSnapshot(snapshot));
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"SparkitectDebug: failed to bind the game-channel model on port {port}.");
                }
            });
        }
        catch (Exception e)
        {
            // Fail loud: a bad port / refused connection is surfaced, never silently swallowed.
            Logger.Error(e, $"SparkitectDebug: failed to open a debug-channel client to port {port}.");
        }
    }
}
