using JetBrains.Annotations;

namespace Sparkitect.Settings;

/// <summary>
/// Non-generic view of a <see cref="SettingDefinition{T}"/>. Readonly sources use it to bind a raw scalar
/// (a CLI argument value or a YAML node) to the setting's declared primitive type without knowing the
/// closed generic. Scalar parse only — never structured or reflection-driven binding.
/// </summary>
[PublicAPI]
public interface ISettingDeclaration
{
    /// <summary>The explicitly declared CLI option key, or null when the setting is not CLI-bound.</summary>
    string? CliOption { get; }

    /// <summary>Parses <paramref name="raw"/> against the declared primitive type.</summary>
    /// <param name="raw">The raw scalar text supplied by a source.</param>
    /// <param name="value">The boxed parsed value on success; otherwise null.</param>
    /// <returns>True when <paramref name="raw"/> parses to the declared type.</returns>
    bool TryParseScalar(string raw, out object? value);
}
