using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Settings.Sources;
using Sparkitect.Utils;

namespace Sparkitect.Settings;

/// <summary>
/// Slim, source-only state module owning the writable user override source. It exists so the future
/// user-settings persistence support (SETG-F01) has a stable module to attach to. The source registration
/// itself is a CoreModule-scoped entry into <see cref="SettingSourceRegistry"/> (an
/// <see cref="IRegistry{TModule}"/> over <see cref="CoreModule"/>), so the user source is resolved into
/// the ordered source list root-wide via the registry bootstrap — its availability is not conditional on
/// this module being loaded on any particular state.
/// </summary>
[ModuleRegistry.RegisterModule("user_settings")]
[PublicAPI]
public partial class UserSettingsModule : IStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    /// <summary>
    /// Registers the writable user override source at mid-precedence (below CLI, above engine-config). The
    /// CLI and engine-config source ids are resolved by their stable string keys so this plan does not take
    /// a compile-time dependency on the sibling sources; the ordering edges are optional, so a build/runtime
    /// where those sources are absent still resolves cleanly.
    /// </summary>
    /// <param name="identifications">Resolves the sibling source ids the user source orders against.</param>
    /// <returns>The writable user source instance.</returns>
    [SettingSourceRegistry.RegisterSource("user")]
    public static ISettingSource CreateUserSource(IIdentificationManager identifications)
    {
        var cliSourceId = identifications.RegisterObject(
            Constants.VirtualSparkitectModId, SettingSourceRegistry.Identifier, "cli");
        var engineConfigSourceId = identifications.RegisterObject(
            Constants.VirtualSparkitectModId, SettingSourceRegistry.Identifier, "engine_config");
        return new UserSettingsSource(cliSourceId, engineConfigSourceId);
    }
}
