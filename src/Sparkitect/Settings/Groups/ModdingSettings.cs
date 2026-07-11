using JetBrains.Annotations;

namespace Sparkitect.Settings.Groups;

/// <summary>
/// The Modding settings group container — the single-owner declaration of the <c>modding</c> group.
/// The accessor generator fills this partial with the group's typed setting members (<c>AlcGranularity</c>,
/// <c>UnloadWaitOnShutdown</c>), each delegating to the hand-written <see cref="ISettingsManager.GetSetting{T}"/>
/// path. Other mods may add their own modding settings by annotating providers with the same group id;
/// a second ownership declaration fails loud.
/// </summary>
[SettingGroup("modding")]
[PublicAPI]
public readonly partial struct ModdingSettings
{
}
