using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Utils.DU;

/// <summary>
/// Three-arm union representing the value cell of a parsed CLI argument: a flag (no value),
/// a single string value, or a list of string values.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record CliArgValue
{
    /// <summary>Flag argument present with no associated value.</summary>
    public sealed partial record Flag() : CliArgValue;

    /// <summary>Argument carrying a single string value.</summary>
    /// <param name="Value">The parsed value.</param>
    public sealed partial record Single(string Value) : CliArgValue;

    /// <summary>Argument carrying multiple string values.</summary>
    /// <param name="Values">The parsed values.</param>
    public sealed partial record Multi(IReadOnlyList<string> Values) : CliArgValue;
}
