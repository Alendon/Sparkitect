using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Marks a type as carrying a compile-time <see cref="Identification"/>. Registered and identified
/// concretes must declare this interface explicitly so source generators can discover the link.
/// </summary>
[PublicAPI]
public interface IHasIdentification
{
    /// <summary>The type's identification, resolved without an instance.</summary>
    public static abstract Identification Identification { get; }
}

/// <summary>
/// Helper to read Identification from types implementing IHasIdentification.
/// Used by generated code to pass identification via type rather than expression.
/// </summary>
[PublicAPI]
public static class IdentificationHelper
{
    /// <summary>Reads the static-abstract <see cref="IHasIdentification.Identification"/> for a compile-time type.</summary>
    public static Identification Read<T>() where T : IHasIdentification => T.Identification;

    /// <summary>
    /// Reflective read of the static-abstract <see cref="IHasIdentification.Identification"/> for a runtime
    /// <see cref="Type"/> known only at runtime (e.g. a generic argument unwrapped from an interface).
    /// </summary>
    public static Identification Read(Type identifiedType) =>
        (Identification)typeof(IdentificationHelper)
            .GetMethod(nameof(Read), Type.EmptyTypes)!
            .MakeGenericMethod(identifiedType)
            .Invoke(null, null)!;
}