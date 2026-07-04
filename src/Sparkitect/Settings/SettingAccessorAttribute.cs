using System;
using JetBrains.Annotations;

namespace Sparkitect.Settings;

/// <summary>
/// Binds a setting declaration to a group's typed accessor. The accessor generator emits
/// <c>settingsManager.&lt;Group&gt;.&lt;Name&gt;</c> returning a <see cref="Setting{T}"/> handle that
/// delegates to the hand-written <see cref="ISettingsManager.GetSetting{T}"/> path — the accessor is
/// pure sugar over that standalone typed API. Place this beside the setting's registry provider
/// attribute on the same <see cref="SettingDefinition{T}"/> provider member; the value type
/// <c>T</c> is recovered from the provider's return type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class SettingAccessorAttribute : Attribute
{
    /// <summary>Binds a setting to a group's typed accessor.</summary>
    /// <param name="group">The owning group id (must match a <see cref="SettingGroupAttribute"/>).</param>
    /// <param name="name">The accessor member name emitted under the group (e.g. <c>Width</c>).</param>
    /// <param name="settingId">The setting id (snake_case) this accessor resolves through the manager.</param>
    public SettingAccessorAttribute(string group, string name, string settingId)
    {
        Group = group;
        Name = name;
        SettingId = settingId;
    }

    /// <summary>The owning group id.</summary>
    public string Group { get; }

    /// <summary>The accessor member name emitted under the group.</summary>
    public string Name { get; }

    /// <summary>The setting id resolved through <see cref="ISettingsManager.GetSetting{T}"/>.</summary>
    public string SettingId { get; }
}
