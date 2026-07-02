using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Utils.DU;

/// <summary>
/// Two-arm tagged union. Holds exactly one of <typeparamref name="T1"/> or <typeparamref name="T2"/>.
/// </summary>
/// <remarks>
/// Bridge type for polymorphic-input use cases that previously relied on <c>OneOf&lt;,&gt;</c>.
/// Slated to migrate to native C# <c>union</c> when that feature stabilises (preview ~.NET 11).
/// Implicit conversions from each arm preserve the OneOf-style ergonomics at call sites.
/// </remarks>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record Variant<T1, T2>
{
    /// <summary>Wraps a <typeparamref name="T1"/> value as the first arm.</summary>
    public static implicit operator Variant<T1, T2>(T1 value) => new Of1(value);

    /// <summary>Wraps a <typeparamref name="T2"/> value as the second arm.</summary>
    public static implicit operator Variant<T1, T2>(T2 value) => new Of2(value);

    /// <summary>First arm holding a <typeparamref name="T1"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of1(T1 Value) : Variant<T1, T2>;

    /// <summary>Second arm holding a <typeparamref name="T2"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of2(T2 Value) : Variant<T1, T2>;
}

/// <summary>
/// Three-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record Variant<T1, T2, T3>
{
    /// <summary>Wraps a <typeparamref name="T1"/> value as the first arm.</summary>
    public static implicit operator Variant<T1, T2, T3>(T1 value) => new Of1(value);

    /// <summary>Wraps a <typeparamref name="T2"/> value as the second arm.</summary>
    public static implicit operator Variant<T1, T2, T3>(T2 value) => new Of2(value);

    /// <summary>Wraps a <typeparamref name="T3"/> value as the third arm.</summary>
    public static implicit operator Variant<T1, T2, T3>(T3 value) => new Of3(value);

    /// <summary>First arm holding a <typeparamref name="T1"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3>;

    /// <summary>Second arm holding a <typeparamref name="T2"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3>;

    /// <summary>Third arm holding a <typeparamref name="T3"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3>;
}

/// <summary>
/// Four-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record Variant<T1, T2, T3, T4>
{
    /// <summary>Wraps a <typeparamref name="T1"/> value as the first arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4>(T1 value) => new Of1(value);

    /// <summary>Wraps a <typeparamref name="T2"/> value as the second arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4>(T2 value) => new Of2(value);

    /// <summary>Wraps a <typeparamref name="T3"/> value as the third arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4>(T3 value) => new Of3(value);

    /// <summary>Wraps a <typeparamref name="T4"/> value as the fourth arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4>(T4 value) => new Of4(value);

    /// <summary>First arm holding a <typeparamref name="T1"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3, T4>;

    /// <summary>Second arm holding a <typeparamref name="T2"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3, T4>;

    /// <summary>Third arm holding a <typeparamref name="T3"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3, T4>;

    /// <summary>Fourth arm holding a <typeparamref name="T4"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of4(T4 Value) : Variant<T1, T2, T3, T4>;
}

/// <summary>
/// Five-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record Variant<T1, T2, T3, T4, T5>
{
    /// <summary>Wraps a <typeparamref name="T1"/> value as the first arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T1 value) => new Of1(value);

    /// <summary>Wraps a <typeparamref name="T2"/> value as the second arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T2 value) => new Of2(value);

    /// <summary>Wraps a <typeparamref name="T3"/> value as the third arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T3 value) => new Of3(value);

    /// <summary>Wraps a <typeparamref name="T4"/> value as the fourth arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T4 value) => new Of4(value);

    /// <summary>Wraps a <typeparamref name="T5"/> value as the fifth arm.</summary>
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T5 value) => new Of5(value);

    /// <summary>First arm holding a <typeparamref name="T1"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3, T4, T5>;

    /// <summary>Second arm holding a <typeparamref name="T2"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3, T4, T5>;

    /// <summary>Third arm holding a <typeparamref name="T3"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3, T4, T5>;

    /// <summary>Fourth arm holding a <typeparamref name="T4"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of4(T4 Value) : Variant<T1, T2, T3, T4, T5>;

    /// <summary>Fifth arm holding a <typeparamref name="T5"/>.</summary>
    /// <param name="Value">The held value.</param>
    public sealed partial record Of5(T5 Value) : Variant<T1, T2, T3, T4, T5>;
}
