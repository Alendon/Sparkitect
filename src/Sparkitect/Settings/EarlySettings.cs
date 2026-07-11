using Sparkitect.Settings.Sources;
using Sparkitect.Utils.DU;

namespace Sparkitect.Settings;

/// <summary>
/// Setting resolution over the two explicit sources that exist outside the settings stack's lifetime:
/// CLI arguments and the working-directory engine config. Values resolve CLI &gt; engine-config &gt;
/// declaration default, parsed against the declaration's own scalar parser. This is the read path for
/// consumers that run before the stack is populated (the pre-container logger, the first mod-group
/// load) or after it is torn down (shutdown unload). Both sources are read once and cached, so every
/// consumer sees one stable value for the process lifetime.
/// </summary>
internal static class EarlySettings
{
    private static readonly Lazy<IReadOnlyDictionary<string, CliArgValue>> CliArgs =
        new(() => CliSettingsSource.ParseArguments(EngineEntryArguments.Args));

    private static readonly Lazy<IReadOnlyDictionary<string, string>> EngineConfig =
        new(EngineSettingsSource.ReadWorkingDirectoryScalars);

    /// <summary>Resolves the effective early value of the setting declared by <paramref name="declaration"/>.</summary>
    /// <param name="engineConfigKey">The setting's engine-config key (its registration name).</param>
    /// <param name="declaration">The declaration supplying the CLI option, scalar parser, and default.</param>
    /// <typeparam name="T">The setting's primitive value type.</typeparam>
    public static T Read<T>(string engineConfigKey, SettingDefinition<T> declaration)
    {
        if (declaration.CliOption is { } cliOption && CliArgs.Value.TryGetValue(cliOption, out var argument))
        {
            var raw = argument switch
            {
                CliArgValue.Flag => "true",
                CliArgValue.Single single => single.Value,
                CliArgValue.Multi multi => multi.Values.Count > 0 ? multi.Values[0] : null,
            };
            if (raw is not null && declaration.TryParseScalar(raw, out var cliValue))
            {
                return (T)cliValue!;
            }
        }

        if (EngineConfig.Value.TryGetValue(engineConfigKey, out var configRaw) &&
            declaration.TryParseScalar(configRaw, out var configValue))
        {
            return (T)configValue!;
        }

        return declaration.Default;
    }
}
