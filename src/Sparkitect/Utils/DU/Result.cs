using JetBrains.Annotations;
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
[PublicAPI]
public abstract partial record Result<TOk, TError>
{
    /// <summary>Wraps a success payload as the <see cref="Ok"/> arm.</summary>
    public static implicit operator Result<TOk, TError>(TOk value) => new Ok(value);

    /// <summary>Wraps a failure payload as the <see cref="Error"/> arm.</summary>
    public static implicit operator Result<TOk, TError>(TError value) => new Error(value);

    /// <summary>Success arm carrying a <typeparamref name="TOk"/> payload.</summary>
    /// <param name="Value">The success payload.</param>
    public sealed partial record Ok(TOk Value) : Result<TOk, TError>;

    /// <summary>Failure arm carrying a <typeparamref name="TError"/> payload.</summary>
    /// <param name="Value">The failure payload.</param>
    public sealed partial record Error(TError Value) : Result<TOk, TError>;
}

/// <summary>
/// Single-arm result type for void-returning fallible operations: success carries no payload,
/// failure carries a <typeparamref name="TError"/>.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record Result<TError>
{
    /// <summary>Wraps a failure payload as the <see cref="Error"/> arm.</summary>
    public static implicit operator Result<TError>(TError value) => new Error(value);

    /// <summary>Success arm; carries no payload.</summary>
    public sealed partial record Ok() : Result<TError>;

    /// <summary>Failure arm carrying a <typeparamref name="TError"/> payload.</summary>
    /// <param name="Value">The failure payload.</param>
    public sealed partial record Error(TError Value) : Result<TError>;
}
