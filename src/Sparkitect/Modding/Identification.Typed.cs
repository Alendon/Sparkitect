using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Phantom-typed <see cref="Identification"/> (still 8 bytes total) carrying a compile-time payload
/// type <typeparamref name="T"/> that is erased at runtime. Implicitly converts to a bare
/// <see cref="Identification"/> (one-way) so all structural <see cref="Identification"/> sites
/// (dictionary keys, <see cref="IIdentificationManager"/>, <see cref="IHasIdentification"/>) accept it unchanged.
/// </summary>
/// <typeparam name="T">The payload type this identification represents.</typeparam>
[PublicAPI]
public readonly struct Identification<T> : IEquatable<Identification<T>>
{
    // Note: [StructLayout(LayoutKind.Explicit)] is not usable here — the CLR throws a
    // TypeLoadException for explicit layout on generic types. The 8-byte guarantee (D-09) is
    // preserved anyway: sole field is the already-explicit-layout, 8-byte Identification, so the
    // default (sequential) layout of this single-field struct is exactly 8 bytes with no padding.
    internal readonly Identification Id;

    /// <summary>
    /// Wraps a bare <see cref="Identification"/> with the phantom payload type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="id">The bare identification to wrap.</param>
    public Identification(Identification id)
    {
        Id = id;
    }

    /// <summary>Implicitly unwraps to the bare <see cref="Identification"/> (one-way; no reverse conversion).</summary>
    public static implicit operator Identification(Identification<T> typed) => typed.Id;

    /// <inheritdoc/>
    public bool Equals(Identification<T> other) => Id.Equals(other.Id);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Identification<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>Returns true when both typed identifications wrap equal inner identifications.</summary>
    public static bool operator ==(Identification<T> left, Identification<T> right) => left.Equals(right);

    /// <summary>Returns true when the wrapped identifications differ.</summary>
    public static bool operator !=(Identification<T> left, Identification<T> right) => !left.Equals(right);

    /// <inheritdoc/>
    public override string ToString() => Id.ToString();
}
