using System.Text.Json;
using Microsoft.Diagnostics.NETCore.Client;
using Serilog;
using Sparkitect.Settings;

namespace Sparkitect.Modding;

/// <summary>
/// Writes a full-heap dump of the running process plus a metadata sidecar when a mod context
/// survives the unload drain, for offline GC-root analysis. At most one dump is written per
/// process: the first leak's capture already contains the state of any later one.
/// </summary>
internal static class LeakHeapDump
{
    private static bool _dumped;

    public static void Write(IReadOnlyList<string> leakedModIds, int drainIterationCap)
    {
        if (_dumped)
        {
            return;
        }

        _dumped = true;

        try
        {
            var logDir = EarlySettings.Read("log_dir", EngineSettingDeclarations.LogDirectory);
            var dumpDir = Path.GetFullPath(Path.Combine(logDir, "leak-dumps"));
            Directory.CreateDirectory(dumpDir);

            var pid = Environment.ProcessId;
            var timestampUtc = DateTime.UtcNow;
            var baseName = $"leak-{timestampUtc:yyyyMMdd-HHmmss}-pid{pid}";
            var dumpPath = Path.Combine(dumpDir, $"{baseName}.dmp");

            Log.Information("Writing leak heap dump to {DumpPath}", dumpPath);
            new DiagnosticsClient(pid).WriteDump(DumpType.WithHeap, dumpPath);

            var metadata = new Metadata(pid, timestampUtc, Environment.Version.ToString(),
                leakedModIds, drainIterationCap);
            File.WriteAllText(Path.Combine(dumpDir, $"{baseName}.json"),
                JsonSerializer.Serialize(metadata, JsonOptions));

            Log.Information("Leak heap dump complete: {DumpPath}", dumpPath);
        }
        catch (Exception e)
        {
            // The dump is observability around an already-reported leak; its failure must not
            // escalate a non-fatal shutdown path.
            Log.Error(e, "Failed to write leak heap dump");
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed record Metadata(
        int ProcessId,
        DateTime TimestampUtc,
        string RuntimeVersion,
        IReadOnlyList<string> LeakedModIds,
        int DrainIterationCap);
}
