namespace Sparkitect.Utilities;

/// <summary>
/// Indicates that string arguments to this parameter must be valid snake_case identifiers.
/// The Roslyn analyzer validates string literals at compile time.
/// </summary>
/// <remarks>
/// Valid snake_case:
/// - Only lowercase letters (a-z), digits (0-9), and underscores
/// - Must start with a letter
/// - No consecutive underscores
/// - No leading or trailing underscores
/// - No dots
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SnakeCaseAttribute : Attribute
{
}
