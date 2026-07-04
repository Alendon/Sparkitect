using JetBrains.Annotations;

namespace Sparkitect.Settings.Groups;

/// <summary>
/// The Window settings group container — the single-owner declaration of the <c>window</c> group. The
/// accessor generator fills this partial with the group's typed setting members (<c>Width</c>,
/// <c>Height</c>), each delegating to the hand-written <see cref="ISettingsManager.GetSetting{T}"/>
/// path.
/// </summary>
[SettingGroup("window")]
[PublicAPI]
public readonly partial struct WindowSettings
{
}
