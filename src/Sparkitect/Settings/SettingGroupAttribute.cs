using System;
using JetBrains.Annotations;

namespace Sparkitect.Settings;

/// <summary>
/// Declares single-owner ownership of a settings group container. A group id is owned by exactly one
/// struct across the whole compilation; the accessor generator fails loud on a second ownership
/// declaration of the same id. Other mods extend an existing group by adding
/// <see cref="SettingAccessorAttribute"/> members that reference the same group id — they do not
/// re-declare ownership.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class SettingGroupAttribute : Attribute
{
    /// <summary>Declares ownership of the group <paramref name="group"/>.</summary>
    /// <param name="group">The group id (snake_case); becomes the PascalCase accessor on the manager.</param>
    public SettingGroupAttribute(string group) => Group = group;

    /// <summary>The owned group id.</summary>
    public string Group { get; }
}
