using JetBrains.Annotations;
using Serilog.Events;

namespace Sparkitect.Settings;

/// <summary>
/// The engine's own hit-list setting declarations. Each provider is registered through the standard
/// <see cref="SettingRegistry"/> path (its default and CLI binding carried by <see cref="SettingDefinition{T}"/>)
/// and bound to a typed group accessor via <see cref="SettingAccessorAttribute"/> so
/// <c>settingsManager.&lt;Group&gt;.&lt;Name&gt;.Value</c> resolves through the manager. Values are
/// primitives only. Window size is a normal engine-declared setting with a default — it is simply not
/// fed by the engine-config source, so <c>Sparkitect.yaml</c> carries no window entry; its override
/// channel is the user source (and CLI where declared).
/// </summary>
[PublicAPI]
public static class EngineSettingDeclarations
{
    /// <summary>Vulkan validation-layer toggle, honored at device creation. Default on.</summary>
    [SettingRegistry.RegisterSetting("vulkan_validation")]
    [SettingAccessor("graphics", "VulkanValidation", "vulkan_validation")]
    public static SettingDefinition<bool> VulkanValidation => new(true, CliOption: "vk-validation");

    /// <summary>VSync / present-mode toggle. Default off, matching the current Mailbox (non-vsync) default.</summary>
    [SettingRegistry.RegisterSetting("vsync")]
    [SettingAccessor("graphics", "VSync", "vsync")]
    public static SettingDefinition<bool> VSync => new(false, CliOption: "vsync");

    /// <summary>Frame-rate cap; 0 = unlimited, matching the current MaxFrameRate default.</summary>
    [SettingRegistry.RegisterSetting("fps_cap")]
    [SettingAccessor("graphics", "FpsCap", "fps_cap")]
    public static SettingDefinition<uint> FpsCap => new(0u, CliOption: "fps-cap");

    /// <summary>Default window width. Not fed by the engine-config source.</summary>
    [SettingRegistry.RegisterSetting("window_width")]
    [SettingAccessor("window", "Width", "window_width")]
    public static SettingDefinition<int> WindowWidth => new(800, CliOption: "width");

    /// <summary>Default window height. Not fed by the engine-config source.</summary>
    [SettingRegistry.RegisterSetting("window_height")]
    [SettingAccessor("window", "Height", "window_height")]
    public static SettingDefinition<int> WindowHeight => new(600, CliOption: "height");

    /// <summary>Minimum log level. Default Debug, matching the current logger configuration.</summary>
    [SettingRegistry.RegisterSetting("log_level")]
    [SettingAccessor("logging", "Level", "log_level")]
    public static SettingDefinition<LogEventLevel> LogLevel => new(LogEventLevel.Debug, CliOption: "log-level");

    /// <summary>Log output directory. Default "logs", matching the current bootstrap constant.</summary>
    [SettingRegistry.RegisterSetting("log_dir")]
    [SettingAccessor("logging", "Directory", "log_dir")]
    public static SettingDefinition<string> LogDirectory => new("logs", CliOption: "log-dir");

    /// <summary>
    /// Debug channel toggle. Default off; opt in via CLI <c>--debug-channel</c> or the engine-config
    /// source. Gates whether the debug composition module is included at all (off ⇒ absent from every
    /// composed set, not merely inert).
    /// </summary>
    [SettingRegistry.RegisterSetting("debug_channel")]
    [SettingAccessor("debug", "ChannelEnabled", "debug_channel")]
    public static SettingDefinition<bool> DebugChannel => new(false, CliOption: "debug-channel");
}
