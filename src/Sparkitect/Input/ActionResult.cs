using JetBrains.Annotations;

namespace Sparkitect.Input;

/// <summary>
/// Per-frame binding-evaluation optional. <see cref="HasValue"/> is <c>false</c> when a binding
/// is not actively contributing (unpressed key, deadzoned stick, dead channel) — never a
/// fabricated <c>default(T)</c>. A zero-allocation <c>readonly struct</c>; calling
/// <see cref="Value()"/> on a <see cref="NoValue"/> result throws. <see cref="OrDefault"/> is the
/// only sanctioned way to collapse a non-contributing result to a concrete value.
/// </summary>
/// <typeparam name="T">The binding's result type.</typeparam>
/// <remarks>
/// The guarded read and the populated-arm factory intentionally share the name <c>Value</c>,
/// disambiguated by arity (mirrors <see cref="Nullable{T}.Value"/>'s guarded-throw precedent for
/// the read side): <c>ActionResult&lt;T&gt;.Value(x)</c> constructs, <c>result.Value()</c> reads.
/// </remarks>
[PublicAPI]
public readonly struct ActionResult<T> : IEquatable<ActionResult<T>>
{
    private readonly T _value;

    /// <summary>True when this result carries an actively-contributing value.</summary>
    public bool HasValue { get; }

    private ActionResult(bool hasValue, T value)
    {
        HasValue = hasValue;
        _value = value;
    }

    /// <summary>The not-actively-contributing result (unpressed key, deadzoned stick, dead channel).</summary>
    public static ActionResult<T> NoValue { get; } = new(false, default!);

    /// <summary>Wraps an actively-contributing value.</summary>
    /// <param name="value">The contributing payload.</param>
    public static ActionResult<T> Value(T value) => new(true, value);

    /// <summary>
    /// The carried value. Throws when <see cref="HasValue"/> is <c>false</c> — never returns
    /// <c>default(T)</c> silently. Use <see cref="OrDefault"/> to opt into a fallback.
    /// </summary>
    public T Value() => HasValue
        ? _value
        : throw new InvalidOperationException("ActionResult has no value; check HasValue or use OrDefault.");

    /// <summary>
    /// Returns the carried value, or <paramref name="fallback"/> when <see cref="HasValue"/> is
    /// false. The only sanctioned way to collapse a non-contributing result.
    /// </summary>
    /// <param name="fallback">The value to return when this result is non-contributing.</param>
    public T OrDefault(T fallback) => HasValue ? _value : fallback;

    /// <inheritdoc/>
    public bool Equals(ActionResult<T> other) =>
        HasValue == other.HasValue && (!HasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ActionResult<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HasValue ? HashCode.Combine(true, _value) : HashCode.Combine(false);

    /// <summary>Returns true when both results carry the same contribution state and payload.</summary>
    public static bool operator ==(ActionResult<T> left, ActionResult<T> right) => left.Equals(right);

    /// <summary>Returns true when the results differ in contribution state or payload.</summary>
    public static bool operator !=(ActionResult<T> left, ActionResult<T> right) => !left.Equals(right);
}

/// <summary>
/// Press/release edge-detection helpers over <see cref="ActionResult{T}"/> for the digital
/// (<see cref="bool"/>) case — the shape the per-frame evaluation loop uses to decide when to
/// publish an edge event off a result slot.
/// </summary>
[PublicAPI]
public static class ActionEdge
{
    /// <summary>
    /// True when <paramref name="current"/> just transitioned into an active-press state:
    /// <paramref name="previous"/> was not <c>Value(true)</c> and <paramref name="current"/> is.
    /// </summary>
    /// <param name="previous">The previous frame's result.</param>
    /// <param name="current">The current frame's result.</param>
    public static bool IsPressEdge(ActionResult<bool> previous, ActionResult<bool> current) =>
        !IsPressed(previous) && IsPressed(current);

    /// <summary>
    /// True when <paramref name="current"/> just transitioned out of an active-press state:
    /// <paramref name="previous"/> was <c>Value(true)</c> and <paramref name="current"/> is not
    /// — including the device vanishing mid-press (<c>Value(true) -&gt; NoValue</c>), which must
    /// not wedge a held action.
    /// </summary>
    /// <param name="previous">The previous frame's result.</param>
    /// <param name="current">The current frame's result.</param>
    public static bool IsReleaseEdge(ActionResult<bool> previous, ActionResult<bool> current) =>
        IsPressed(previous) && !IsPressed(current);

    private static bool IsPressed(ActionResult<bool> result) => result.HasValue && result.Value();
}
