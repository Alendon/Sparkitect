using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Settings;

/// <summary>
/// State module that drives the settings registration passes: it processes the setting and
/// setting-source registries on state entry and exit. Depends on the core module, which owns the
/// registries and the manager. The callback-teardown state function is added in a later plan.
/// </summary>
[ModuleRegistry.RegisterModule("settings")]
[PublicAPI]
public partial class SettingsModule : IStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    [OnFrameEnterScheduling]
    [TransitionFunction("process_settings_registries_up")]
    static void ProcessRegistriesUp(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<SettingRegistry, CoreModule>();
        registryManager.ProcessRegistry<SettingSourceRegistry, CoreModule>();
    }

    [OnFrameExitScheduling]
    [TransitionFunction("process_settings_registries_down")]
    static void ProcessRegistriesDown(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<SettingRegistry, CoreModule>();
        registryManager.ProcessRegistry<SettingSourceRegistry, CoreModule>();
    }

    // Playback for the record-only RegisterSource: recomputes the resolution order once after the
    // frame's whole registry pass, so a source's required order edges may target sources registered
    // later in the same pass.
    [OnFrameEnterScheduling]
    [TransitionFunction("process_registered_setting_sources")]
    [OrderAfter<ProcessSettingsRegistriesUpFunc>]
    static void ProcessRegisteredSources(ISettingsManager manager)
    {
        if (manager is SettingsManager settingsManager)
            settingsManager.ProcessRegisteredSources();
    }

    // Binds new subscriptions to the current leaf frame's container identity (D-15). This module is
    // root-loaded and thus ambiently scheduled (D-22), so this runs on every frame enter and keeps the
    // provider pointed at the live leaf container; ClearSubscriptions below tears down that frame's bag.
    [OnFrameEnterScheduling]
    [TransitionFunction("wire_settings_frame_token")]
    static void WireFrameToken(ISettingsManager manager, IGameStateManager gameStateManager)
    {
        if (manager is SettingsManager settingsManager)
            settingsManager.UseFrameTokenProvider(() => gameStateManager.CurrentCoreContainer);
    }

    // Frame-exit teardown trigger (D-21): clears the exiting frame's subscriptions so they never outlive
    // the frame (rooting-leak mitigation). The exit runs while the exiting frame is still the leaf, so
    // CurrentCoreContainer is that frame's container — the same token WireFrameToken bound at subscribe time.
    [OnFrameExitScheduling]
    [TransitionFunction("clear_settings_subscriptions")]
    static void ClearFrameSubscriptions(ISettingsManager manager, IGameStateManager gameStateManager)
    {
        if (manager is SettingsManager settingsManager)
            settingsManager.ClearSubscriptionsForFrame(gameStateManager.CurrentCoreContainer);
    }
}
