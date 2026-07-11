using JetBrains.Annotations;
using Silk.NET.Input;

namespace Sparkitect.WindowInput;

/// <summary>
/// Binding-backing setting for <see cref="KeyboardKey"/>: the single bound key, stored directly
/// as the Silk.NET <see cref="Key"/> enum (no string encoding — D-17).
/// </summary>
/// <param name="Key">The bound key.</param>
[PublicAPI]
public readonly record struct KeyboardKeySetting(Key Key);

/// <summary>
/// Binding-backing setting for <see cref="KeyboardAxis"/>: the two keys mapped to the axis'
/// negative/positive extremes, both stored directly as <see cref="Key"/> enum values
/// (no string encoding — D-17).
/// </summary>
/// <param name="Negative">The key that drives the axis to -1.</param>
/// <param name="Positive">The key that drives the axis to +1.</param>
[PublicAPI]
public readonly record struct KeyboardAxisSetting(Key Negative, Key Positive);

/// <summary>
/// Binding-backing setting for <see cref="KeyboardVector2"/>: the four keys (WASD-shaped) that
/// compose into a 2D vector. This is the codebase's FIRST composite value-type setting (R-6) —
/// one atomic <c>readonly struct</c> of four keys, stored directly as <see cref="Key"/> enum
/// values (no string encoding — D-17), rebound as a single atomic write.
/// </summary>
/// <param name="Up">The key that drives the vector's Y toward -1.</param>
/// <param name="Down">The key that drives the vector's Y toward +1.</param>
/// <param name="Left">The key that drives the vector's X toward -1.</param>
/// <param name="Right">The key that drives the vector's X toward +1.</param>
[PublicAPI]
public readonly record struct KeyboardVector2Setting(Key Up, Key Down, Key Left, Key Right);
