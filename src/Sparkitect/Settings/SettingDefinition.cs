using System.Globalization;
using JetBrains.Annotations;

namespace Sparkitect.Settings;

/// <summary>
/// The declaration payload for a setting: its default value and, optionally, the CLI option that feeds
/// it. This is the closed-generic value carried through the standard registry registration path
/// (a provider returns <see cref="SettingDefinition{T}"/> and the generator preserves the closed
/// generic type).
/// </summary>
/// <remarks>
/// Setting values are primitives only (bool, int, float, enum, string). Compound values are modelled as
/// separate primitive settings — there is no structured-value surface. <typeparamref name="T"/> is left
/// unconstrained so a single carrier covers both the unmanaged primitives and <see cref="string"/>;
/// primitives-only is a declaration convention, not a type constraint.
/// </remarks>
/// <typeparam name="T">The primitive value type of the setting.</typeparam>
[PublicAPI]
public sealed record SettingDefinition<T> : ISettingDeclaration
{
    /// <summary>Creates a setting declaration.</summary>
    /// <param name="Default">The value resolved when no source supplies an explicit value.</param>
    /// <param name="CliOption">
    /// The CLI option key that feeds this setting, or null when no CLI binding is declared. A setting
    /// must explicitly declare its CLI option — there is no name-derived auto-binding. The <c>no-</c>
    /// prefix is reserved for CLI negation and rejected here.
    /// </param>
    public SettingDefinition(T Default, string? CliOption = null)
    {
        if (CliOption?.StartsWith("no-", StringComparison.Ordinal) == true)
        {
            throw new ArgumentException(
                $"CLI option '{CliOption}' starts with the reserved negation prefix 'no-'.", nameof(CliOption));
        }

        this.Default = Default;
        this.CliOption = CliOption;
    }

    /// <summary>The value resolved when no source supplies an explicit value.</summary>
    public T Default { get; }

    /// <summary>The explicitly declared CLI option key, or null when the setting is not CLI-bound.</summary>
    public string? CliOption { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Scalar parse against <typeparamref name="T"/> only: string is taken verbatim, enums parse
    /// case-insensitively, bool via <see cref="bool.TryParse(string, out bool)"/>, and the numeric
    /// primitives via <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> under the invariant
    /// culture. There is no structured or reflection-driven object binding.
    /// </remarks>
    public bool TryParseScalar(string raw, out object? value)
    {
        var type = typeof(T);
        try
        {
            if (type == typeof(string))
            {
                value = raw;
                return true;
            }

            if (type.IsEnum)
            {
                value = Enum.Parse(type, raw, ignoreCase: true);
                return true;
            }

            if (type == typeof(bool))
            {
                if (bool.TryParse(raw, out var parsed))
                {
                    value = parsed;
                    return true;
                }

                value = null;
                return false;
            }

            value = Convert.ChangeType(raw, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException
                                              or OverflowException or ArgumentException)
        {
            value = null;
            return false;
        }
    }
}
