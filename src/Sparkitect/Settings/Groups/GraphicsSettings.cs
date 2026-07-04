using JetBrains.Annotations;

namespace Sparkitect.Settings.Groups;

/// <summary>
/// The Graphics settings group container — the single-owner declaration of the <c>graphics</c> group.
/// The accessor generator fills this partial with the group's typed setting members (e.g.
/// <c>VulkanValidation</c>, <c>VSync</c>, <c>FpsCap</c>), each delegating to the hand-written
/// <see cref="ISettingsManager.GetSetting{T}"/> path. Other mods may add their own graphics settings by
/// annotating providers with the same group id; a second ownership declaration fails loud.
/// </summary>
[SettingGroup("graphics")]
[PublicAPI]
public readonly partial struct GraphicsSettings
{
}
