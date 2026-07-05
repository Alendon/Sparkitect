using Sparkitect.Debug;

namespace Sparkitect.Tests.Debug;

/// <summary>
/// Pins the discovery-file contract the plugin watcher reads: one file per pid named <c>{pid}.json</c> under
/// the shared-temp subdir, carrying a flat <c>{"pid","port","engineVersion"}</c> object, written on start and
/// removed on shutdown. Pure file lifecycle — no socket. Tests the produced file, never log side effects.
/// </summary>
public class DiscoveryFileTests
{
    // Synthetic pids well outside any real process range, distinct per test to keep parallel runs isolated.
    private const int PidRoundTrip = 2_000_000_001;
    private const int PidNaming = 2_000_000_002;
    private const int PidDelete = 2_000_000_003;
    private const int PidFormat = 2_000_000_004;

    [Test]
    public async Task Write_ThenParse_RoundTripsPidPortVersion()
    {
        var path = DebugDiscoveryFile.Write(PidRoundTrip, 51234, "1.0.0");
        try
        {
            var parsed = DebugDiscoveryFile.Parse(path);
            await Assert.That(parsed.Pid).IsEqualTo(PidRoundTrip);
            await Assert.That(parsed.Port).IsEqualTo(51234);
            await Assert.That(parsed.EngineVersion).IsEqualTo("1.0.0");
        }
        finally
        {
            DebugDiscoveryFile.Delete(path);
        }
    }

    [Test]
    public async Task Write_UsesPidNamedFileInSharedTempSubdir()
    {
        var path = DebugDiscoveryFile.Write(PidNaming, 40000, "1.0.0");
        try
        {
            await Assert.That(File.Exists(path)).IsTrue();
            await Assert.That(Path.GetFileName(path)).IsEqualTo($"{PidNaming}.json");
            var baseDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR") is { Length: > 0 } runtime
                ? runtime
                : Path.GetTempPath();
            var expectedDir = Path.Combine(baseDir, "Sparkitect", "debug-channel");
            await Assert.That(Path.GetDirectoryName(path)).IsEqualTo(expectedDir);
        }
        finally
        {
            DebugDiscoveryFile.Delete(path);
        }
    }

    [Test]
    public async Task Delete_RemovesThePidNamedFile()
    {
        var path = DebugDiscoveryFile.Write(PidDelete, 40001, "1.0.0");
        await Assert.That(File.Exists(path)).IsTrue();

        DebugDiscoveryFile.Delete(path);

        await Assert.That(File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task Written_Json_HasFlatPidPortEngineVersionKeys()
    {
        var path = DebugDiscoveryFile.Write(PidFormat, 40002, "1.0.0");
        try
        {
            var text = File.ReadAllText(path);
            await Assert.That(text).Contains($"\"pid\":{PidFormat}");
            await Assert.That(text).Contains("\"port\":40002");
            await Assert.That(text).Contains("\"engineVersion\":\"1.0.0\"");
        }
        finally
        {
            DebugDiscoveryFile.Delete(path);
        }
    }
}
