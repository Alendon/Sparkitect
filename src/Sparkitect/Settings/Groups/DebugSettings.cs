using JetBrains.Annotations;

namespace Sparkitect.Settings.Groups;

/// <summary>
/// The Debug settings group container — the single-owner declaration of the <c>debug</c> group. The
/// accessor generator fills this partial with the group's typed setting members (e.g.
/// <c>ChannelEnabled</c>), each delegating to the hand-written <see cref="ISettingsManager.GetSetting{T}"/>
/// path. Other mods may add their own debug settings by annotating providers with the same group id; a
/// second ownership declaration fails loud.
/// </summary>
[SettingGroup("debug")]
[PublicAPI]
public readonly partial struct DebugSettings
{
}
