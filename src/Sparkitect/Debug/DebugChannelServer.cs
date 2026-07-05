using System.Net;
using System.Text.Json;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Impl;
using Serilog;
using Sparkitect.Debug.Protocol;
using Sparkitect.Debug.Protocol.Game;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Utils;
using RdProtocol = JetBrains.Rd.Impl.Protocol;

namespace Sparkitect.Debug;

/// <summary>
/// The engine end of the debug channel. Brings up a rd <see cref="SocketWire.Server"/> the first time it is
/// asked to publish, then rebuilds and pushes the <see cref="DebugSnapshot"/> to the connected client.
/// The owning module drives it from the game main thread.
/// </summary>
internal interface IDebugChannelServer
{
    /// <summary>
    /// Brings the channel online on first call (idempotent), then rebuilds the snapshot from the live
    /// manager and pushes it to the connected client. Call on every composition change.
    /// </summary>
    void Republish();
}

/// <summary>
/// Hosts the debug channel: a rd <see cref="SocketWire.Server"/> on an OS-assigned loopback port, a
/// per-connection game-channel model, and the discovery file the plugin watcher reads. A process-level
/// subsystem — bound to <see cref="CoreModule"/> only so it resolves in the root container the gated
/// <see cref="DebugChannelModule"/> transition functions run in (that module never enters a frame delta, so
/// it cannot carry its own service). Nothing is hosted until <see cref="Republish"/> is first called, which
/// only happens once the module composed (setting on) and the bootstrapper has completed.
/// </summary>
[StateService<IDebugChannelServer, CoreModule>]
internal sealed class DebugChannelServer : IDebugChannelServer, IDisposable
{
    public required IGameStateManager GameStateManager { get; init; }
    public required IIdentificationManager Identifications { get; init; }

    private readonly LifetimeDefinition _lifetimeDef = new();

    private IScheduler? _scheduler;

    // Touched only on the wire scheduler thread (connect handler + queued republish).
    private SparkitectDebugModel? _model;

    // Built on the game thread, read on the scheduler thread; the snapshot itself is immutable.
    private volatile DebugSnapshot? _lastSnapshot;

    private string? _discoveryPath;
    private bool _started;

    /// <inheritdoc />
    public void Republish()
    {
        EnsureStarted();

        var snapshot = DebugSnapshotBuilder.Build(GameStateManager, Identifications);
        _lastSnapshot = snapshot;
        _scheduler?.Queue(() =>
        {
            var model = _model;
            if (model != null)
                model.Snapshot.Value = snapshot;
        });
    }

    /// <summary>Takes the channel offline (called on container teardown): terminates the wire, removes the file.</summary>
    public void Dispose() => _lifetimeDef.Terminate();

    // Hosts the wire, wires the per-connection model, and writes the discovery file. Idempotent. Fails loud:
    // a host error tears the lifetime down and rethrows rather than leaving a half-open channel.
    private void EnsureStarted()
    {
        if (_started)
            return;

        try
        {
            RdSerilogLogFactory.Install();

            var lifetime = _lifetimeDef.Lifetime;
            var scheduler = SingleThreadScheduler.RunOnSeparateThread(lifetime, "SparkitectDebug", _ => { });
            _scheduler = scheduler;

            var wire = new SocketWire.Server(lifetime, scheduler, new IPEndPoint(IPAddress.Loopback, 0), "SparkitectDebug");
            var protocol = new RdProtocol("SparkitectDebug", new Serializers(), new Identities(IdKind.Server),
                scheduler, wire, lifetime);

            wire.Connected.WhenTrue(lifetime, connectionLifetime =>
            {
                var model = new SparkitectDebugModel(connectionLifetime, protocol);
                _model = model;
                connectionLifetime.OnTermination(() =>
                {
                    if (ReferenceEquals(_model, model))
                        _model = null;
                });

                // Push what the game thread last built, so a fresh client is current immediately.
                model.Snapshot.Value = _lastSnapshot;
            });

            var port = wire.Port;
            _discoveryPath = DebugDiscoveryFile.Write(Environment.ProcessId, port, Constants.VirtualSparkitectVersion);
            lifetime.OnTermination(() => DebugDiscoveryFile.Delete(_discoveryPath!));

            _started = true;
            Log.Information("Debug channel online on 127.0.0.1:{Port} (discovery {Path})", port, _discoveryPath);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Debug channel failed to start");
            _lifetimeDef.Terminate();
            throw;
        }
    }
}

/// <summary>
/// The discovery rendezvous both the engine (writer) and the plugin watcher (reader) agree on: one file per
/// pid named <c>{pid}.json</c> under a fixed shared-temp subdirectory, carrying a flat
/// <c>{"pid","port","engineVersion"}</c> object. Written on channel start, removed on shutdown.
/// </summary>
internal static class DebugDiscoveryFile
{
    /// <summary>The shared per-user directory the plugin watches (mirrors the plugin's reader).</summary>
    public static readonly string Directory =
        Path.Combine(ResolveBaseDirectory(), "Sparkitect", "debug-channel");

    // TMPDIR (which GetTempPath honors) varies per shell on Linux, so writer and reader could
    // rendezvous in different directories. XDG_RUNTIME_DIR is per-user and session-stable;
    // fall back to the OS temp path where it is not set (Windows/macOS).
    private static string ResolveBaseDirectory()
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        return string.IsNullOrEmpty(runtime) ? Path.GetTempPath() : runtime;
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>The discovery file path for a given pid.</summary>
    public static string PathFor(int pid) => Path.Combine(Directory, $"{pid}.json");

    /// <summary>Writes the discovery file for this channel and returns its path.</summary>
    public static string Write(int pid, int port, string engineVersion)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var path = PathFor(pid);
        File.WriteAllText(path, JsonSerializer.Serialize(new DiscoveryFileInfo(pid, port, engineVersion), Json));
        return path;
    }

    /// <summary>Reads a discovery file back into its record (the reader's inverse; used for verification).</summary>
    public static DiscoveryFileInfo Parse(string path) =>
        JsonSerializer.Deserialize<DiscoveryFileInfo>(File.ReadAllText(path), Json)
        ?? throw new InvalidOperationException($"Discovery file '{path}' did not parse to a channel record.");

    /// <summary>Best-effort removal on shutdown; a leftover file is treated as stale by the watcher.</summary>
    public static void Delete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Debug channel: could not remove discovery file {Path}", path);
        }
    }
}

/// <summary>A parsed discovery record: the debuggee pid, its rd port, and the engine version marker.</summary>
internal sealed record DiscoveryFileInfo(int Pid, int Port, string EngineVersion);
