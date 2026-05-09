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
public abstract partial record Variant<T1, T2>
{
    public static implicit operator Variant<T1, T2>(T1 value) => new Of1(value);
    public static implicit operator Variant<T1, T2>(T2 value) => new Of2(value);

    public sealed partial record Of1(T1 Value) : Variant<T1, T2>;
    public sealed partial record Of2(T2 Value) : Variant<T1, T2>;
}

/// <summary>
/// Three-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
public abstract partial record Variant<T1, T2, T3>
{
    public static implicit operator Variant<T1, T2, T3>(T1 value) => new Of1(value);
    public static implicit operator Variant<T1, T2, T3>(T2 value) => new Of2(value);
    public static implicit operator Variant<T1, T2, T3>(T3 value) => new Of3(value);

    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3>;
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3>;
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3>;
}

/// <summary>
/// Four-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
public abstract partial record Variant<T1, T2, T3, T4>
{
    public static implicit operator Variant<T1, T2, T3, T4>(T1 value) => new Of1(value);
    public static implicit operator Variant<T1, T2, T3, T4>(T2 value) => new Of2(value);
    public static implicit operator Variant<T1, T2, T3, T4>(T3 value) => new Of3(value);
    public static implicit operator Variant<T1, T2, T3, T4>(T4 value) => new Of4(value);

    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3, T4>;
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3, T4>;
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3, T4>;
    public sealed partial record Of4(T4 Value) : Variant<T1, T2, T3, T4>;
}

/// <summary>
/// Five-arm tagged union.
/// </summary>
/// <remarks>See <see cref="Variant{T1,T2}"/> for migration context.</remarks>
[DiscriminatedUnion]
public abstract partial record Variant<T1, T2, T3, T4, T5>
{
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T1 value) => new Of1(value);
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T2 value) => new Of2(value);
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T3 value) => new Of3(value);
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T4 value) => new Of4(value);
    public static implicit operator Variant<T1, T2, T3, T4, T5>(T5 value) => new Of5(value);

    public sealed partial record Of1(T1 Value) : Variant<T1, T2, T3, T4, T5>;
    public sealed partial record Of2(T2 Value) : Variant<T1, T2, T3, T4, T5>;
    public sealed partial record Of3(T3 Value) : Variant<T1, T2, T3, T4, T5>;
    public sealed partial record Of4(T4 Value) : Variant<T1, T2, T3, T4, T5>;
    public sealed partial record Of5(T5 Value) : Variant<T1, T2, T3, T4, T5>;
}
