using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Settings.Sources;

namespace Sparkitect.Settings;

/// <summary>
/// CoreModule's two readonly setting sources: the CLI source and the engine-config
/// (<c>Sparkitect.yaml</c>) source. Both register into <see cref="SettingSourceRegistry"/> with
/// ordering metadata placing CLI ahead of engine-config. The user source inserts itself
/// between them via its own <see cref="ISettingSource.OrderAfter"/>/<see cref="ISettingSource.OrderBefore"/>
/// edges against these two ids, yielding the full <c>CLI &gt; user &gt; engine-config &gt; defaults</c> order.
/// </summary>
[PublicAPI]
public static class CoreSettingSources
{
    /// <summary>The readonly CLI source, ordered ahead of the engine-config source.</summary>
    /// <param name="manager">Resolves setting declarations (CLI option + scalar parser).</param>
    // The order-edge lambda defers reading SettingSourceID.Sparkitect.EngineConfig until the registration
    // pass is processed: registrations run sorted by name, so the id is still unassigned when Cli() runs.
    [SettingSourceRegistry.RegisterSource("cli")]
    public static ISettingSource Cli(ISettingsManager manager) =>
        new CliSettingsSource(
            EntryArgs(),
            manager.GetDeclaration,
            orderBefore: static () => [new SettingSourceOrder(SettingSourceID.Sparkitect.EngineConfig)]);

    /// <summary>The readonly engine-config source reading <c>Sparkitect.yaml</c> from the working directory.</summary>
    /// <param name="manager">Resolves setting declarations (scalar parser).</param>
    /// <param name="identifications">Resolves a setting id to its registration name (the YAML key).</param>
    [SettingSourceRegistry.RegisterSource("engine_config")]
    public static ISettingSource EngineConfig(ISettingsManager manager, IIdentificationManager identifications) =>
        EngineSettingsSource.FromWorkingDirectory(
            keyProvider: id => identifications.TryResolveIdentification(id, out _, out _, out var objectId) ? objectId : null,
            declarationProvider: manager.GetDeclaration);

    // The engine entry arguments, threaded explicitly from EngineBootstrapper.Main via EngineEntryArguments.
    private static IReadOnlyList<string> EntryArgs() => EngineEntryArguments.Args;
}
