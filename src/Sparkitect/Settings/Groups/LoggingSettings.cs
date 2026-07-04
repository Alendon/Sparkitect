using JetBrains.Annotations;

namespace Sparkitect.Settings.Groups;

/// <summary>
/// The Logging settings group container — the single-owner declaration of the <c>logging</c> group. The
/// accessor generator fills this partial with the group's typed setting members (<c>Level</c>,
/// <c>Directory</c>), each delegating to the hand-written <see cref="ISettingsManager.GetSetting{T}"/>
/// path.
/// </summary>
[SettingGroup("logging")]
[PublicAPI]
public readonly partial struct LoggingSettings
{
}
