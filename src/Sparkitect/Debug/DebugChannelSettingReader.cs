using Sparkitect.Settings;
using Sparkitect.Settings.Sources;
using Sparkitect.Utils.DU;

namespace Sparkitect.Debug;

/// <summary>
/// Early, pre-frame read of the <c>debug_channel</c> toggle (resolution order CLI &gt; engine-config &gt;
/// default), mirroring the bootstrapper's logger early-read. The composition gate runs during
/// registration finalize, before any state frame is entered — at which point the container's CLI and
/// engine-config setting sources are not yet registered, so the ambient settings manager would resolve
/// only the default. This reads those two sources directly so a <c>--debug-channel</c> opt-in is honored
/// at the gate. The user (writable) source is intentionally not consulted here, matching the engine's
/// established early-read convention.
/// </summary>
internal static class DebugChannelSettingReader
{
    /// <summary>Resolves whether the debug channel is enabled from CLI then engine-config, else the default.</summary>
    public static bool ReadEnabled()
    {
        var declaration = EngineSettingDeclarations.DebugChannel;
        var cliArgs = CliSettingsSource.ParseArguments(EngineEntryArguments.Args);

        if (declaration.CliOption is { } cliOption && cliArgs.TryGetValue(cliOption, out var argument))
        {
            var raw = argument switch
            {
                CliArgValue.Flag => "true",
                CliArgValue.Single single => single.Value,
                CliArgValue.Multi multi => multi.Values.Count > 0 ? multi.Values[0] : null,
            };
            if (raw is not null && declaration.TryParseScalar(raw, out var cliValue) && cliValue is bool cliBool)
            {
                return cliBool;
            }
        }

        var engineConfig = EngineSettingsSource.ReadWorkingDirectoryScalars();
        if (engineConfig.TryGetValue("debug_channel", out var configRaw) &&
            declaration.TryParseScalar(configRaw, out var configValue) && configValue is bool configBool)
        {
            return configBool;
        }

        return declaration.Default;
    }
}
