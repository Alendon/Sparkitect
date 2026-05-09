using Sundew.DiscriminatedUnions;

namespace Sparkitect.Utils.DU;

/// <summary>
/// Three-arm union representing the value cell of a parsed CLI argument: a flag (no value),
/// a single string value, or a list of string values.
/// </summary>
[DiscriminatedUnion]
public abstract partial record CliArgValue
{
    public sealed partial record Flag() : CliArgValue;
    public sealed partial record Single(string Value) : CliArgValue;
    public sealed partial record Multi(IReadOnlyList<string> Values) : CliArgValue;
}
