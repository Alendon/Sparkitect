using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Lifetimes;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.Debug;

/// <summary>A live Sparkitect debug channel discovered from a shared-temp discovery file: its process id,
/// the OS-assigned rd port to connect to, and the engine version for the loud cross-process handshake.</summary>
public sealed class DiscoveredProcess
{
    public DiscoveredProcess(int pid, int port, string engineVersion)
    {
        Pid = pid;
        Port = port;
        EngineVersion = engineVersion;
    }

    /// <summary>The debuggee process id (the discovery file is named <c>{pid}.json</c>).</summary>
    public int Pid { get; }

    /// <summary>The OS-assigned port the engine's rd <c>SocketWire.Server</c> listens on.</summary>
    public int Port { get; }

    /// <summary>The engine version marker; the client rejects a version it does not speak (D-09).</summary>
    public string EngineVersion { get; }
}

/// <summary>
/// Watches the shared-temp discovery directory and enumerates the live Sparkitect debug channels for the
/// process selector (D-07). Each running game writes one file per pid on channel start and removes it on
/// shutdown (D-16; engine side is plan 06). This watcher parses those files into a process list and treats
/// a file whose pid is no longer alive as stale (crash leftovers), so the selector never offers a dead
/// channel. All resources are scoped to the supplied <see cref="Lifetime" />. A parse/read error is logged
/// loudly and the offending file skipped (a half-written file re-fires a change event) — the watcher itself
/// never goes down silently.
/// </summary>
/// <remarks>
/// Discovery contract (this watcher is the reader; plan 06 is the writer and MUST match):
/// <list type="bullet">
///   <item>Directory: <c>{XDG_RUNTIME_DIR|OS-temp}/Sparkitect/debug-channel/</c> (see <see cref="DiscoveryDirectory" />).</item>
///   <item>File name: <c>{pid}.json</c>, one per running channel.</item>
///   <item>Content: a flat JSON object <c>{"pid":&lt;int&gt;,"port":&lt;int&gt;,"engineVersion":"&lt;string&gt;"}</c>.</item>
/// </list>
/// The reader is tolerant: unknown extra keys are ignored, and the numeric pid in the file name is the
/// authoritative liveness key (a mismatched in-file pid does not resurrect a dead file).
/// </remarks>
public sealed class DiscoveryWatcher
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(DiscoveryWatcher));

    /// <summary>The shared per-user rendezvous directory both the engine and the plugin agree on (D-16).</summary>
    public static readonly string DiscoveryDirectory =
        Path.Combine(ResolveBaseDirectory(), "Sparkitect", "debug-channel");

    // TMPDIR (which GetTempPath honors) varies per shell on Linux, so writer and reader could
    // rendezvous in different directories. XDG_RUNTIME_DIR is per-user and session-stable;
    // fall back to the OS temp path where it is not set (Windows/macOS).
    private static string ResolveBaseDirectory()
    {
        var runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        return string.IsNullOrEmpty(runtime) ? Path.GetTempPath() : runtime;
    }

    private static readonly Regex PidField = MakeIntField("pid");
    private static readonly Regex PortField = MakeIntField("port");
    private static readonly Regex VersionField =
        new(@"""engineVersion""\s*:\s*""(?<v>[^""]*)""", RegexOptions.Compiled);

    private readonly object myLock = new();
    private IReadOnlyList<DiscoveredProcess> myProcesses = new List<DiscoveredProcess>();

    /// <summary>Raised (on a background thread) whenever the discovered-process list may have changed.</summary>
    public event Action? Changed;

    public DiscoveryWatcher(Lifetime lifetime)
    {
        // The writer creates the directory on channel start; create it here too so the watcher can attach
        // before any game has run (harmless, idempotent).
        try
        {
            Directory.CreateDirectory(DiscoveryDirectory);
        }
        catch (Exception e)
        {
            Logger.Warn($"DiscoveryWatcher: could not ensure discovery directory '{DiscoveryDirectory}': {e.Message}");
        }

        var watcher = new FileSystemWatcher(DiscoveryDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        watcher.Created += OnChanged;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnChanged;
        watcher.EnableRaisingEvents = true;
        lifetime.OnTermination(() => watcher.Dispose());

        // A process dying without deleting its file (crash, kill) produces no filesystem event, so the
        // pid-liveness filter alone would keep offering the dead channel. A low-frequency sweep re-runs
        // the filter between events.
        var sweep = new System.Threading.Timer(_ => Rescan(), null, LivenessSweepInterval, LivenessSweepInterval);
        lifetime.OnTermination(() => sweep.Dispose());

        Rescan();
    }

    private static readonly TimeSpan LivenessSweepInterval = TimeSpan.FromSeconds(2);

    /// <summary>The currently live discovered processes (dead-pid files excluded). Immutable snapshot.</summary>
    public IReadOnlyList<DiscoveredProcess> Processes
    {
        get
        {
            lock (myLock)
                return myProcesses;
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => Rescan();

    private void Rescan()
    {
        var found = new List<DiscoveredProcess>();

        string[] files;
        try
        {
            files = Directory.Exists(DiscoveryDirectory)
                ? Directory.GetFiles(DiscoveryDirectory, "*.json")
                : Array.Empty<string>();
        }
        catch (Exception e)
        {
            Logger.Warn($"DiscoveryWatcher: could not enumerate '{DiscoveryDirectory}': {e.Message}");
            return;
        }

        foreach (var file in files)
        {
            var process = TryParse(file);
            if (process == null)
                continue;

            if (!IsProcessAlive(process.Pid))
                continue; // stale crash leftover — the selector must not offer a dead channel (D-07)

            found.Add(process);
        }

        found.Sort((a, b) => a.Pid.CompareTo(b.Pid));

        lock (myLock)
        {
            // The liveness sweep rescans on a timer; only a real delta is worth republishing.
            var unchanged = myProcesses.Count == found.Count
                && myProcesses.Zip(found, (a, b) =>
                    a.Pid == b.Pid && a.Port == b.Port && a.EngineVersion == b.EngineVersion).All(x => x);
            if (unchanged)
                return;

            myProcesses = found;
        }

        Changed?.Invoke();
    }

    private static DiscoveredProcess? TryParse(string file)
    {
        string text;
        try
        {
            text = File.ReadAllText(file);
        }
        catch (Exception e)
        {
            // Transient — the writer may be mid-write; the completing write re-fires a change event.
            Logger.Warn($"DiscoveryWatcher: could not read discovery file '{file}': {e.Message}");
            return null;
        }

        var pidMatch = PidField.Match(text);
        var portMatch = PortField.Match(text);
        var versionMatch = VersionField.Match(text);
        if (!pidMatch.Success || !portMatch.Success || !versionMatch.Success)
        {
            Logger.Warn($"DiscoveryWatcher: discovery file '{file}' is missing pid/port/engineVersion; skipping.");
            return null;
        }

        var pid = int.Parse(pidMatch.Groups["n"].Value, CultureInfo.InvariantCulture);
        var port = int.Parse(portMatch.Groups["n"].Value, CultureInfo.InvariantCulture);
        return new DiscoveredProcess(pid, port, versionMatch.Groups["v"].Value);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false; // no process with that id
        }
        catch (InvalidOperationException)
        {
            return false; // already exited between lookup and query
        }
    }

    private static Regex MakeIntField(string name) =>
        new($@"""{name}""\s*:\s*(?<n>-?\d+)", RegexOptions.Compiled);
}
