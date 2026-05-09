using Sundew.DiscriminatedUnions;

namespace Sparkitect.Utils.DU;

/// <summary>
/// Two-arm result type. Holds either a success payload of type <typeparamref name="TOk"/>
/// or a failure payload of type <typeparamref name="TError"/>.
/// </summary>
/// <remarks>
/// Workhorse Result for fallible operations. Slated to migrate to native C# <c>union</c>
/// when that feature stabilises (preview ~.NET 11).
/// </remarks>
[DiscriminatedUnion]
public abstract partial record Result<TOk, TError>
{
    public static implicit operator Result<TOk, TError>(TOk value) => new Ok(value);
    public static implicit operator Result<TOk, TError>(TError value) => new Error(value);

    public sealed partial record Ok(TOk Value) : Result<TOk, TError>;
    public sealed partial record Error(TError Value) : Result<TOk, TError>;
}

/// <summary>
/// Single-arm result type for void-returning fallible operations: success carries no payload,
/// failure carries a <typeparamref name="TError"/>.
/// </summary>
[DiscriminatedUnion]
public abstract partial record Result<TError>
{
    public static implicit operator Result<TError>(TError value) => new Error(value);

    public sealed partial record Ok() : Result<TError>;
    public sealed partial record Error(TError Value) : Result<TError>;
}
