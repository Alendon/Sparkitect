using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Canonical builder for <see cref="ILazyIdentification"/>. Wraps a compile-time
/// <see cref="IHasIdentification"/> type as a deferred, no-cache identification reference.
/// </summary>
[PublicAPI]
public static class LazyIdentification
{
    /// <summary>Builds a lazy reference that resolves to <typeparamref name="T"/>'s identification on each call.</summary>
    public static ILazyIdentification Of<T>() where T : IHasIdentification => new OfImpl<T>();

    private readonly struct OfImpl<T> : ILazyIdentification where T : IHasIdentification
    {
        public Identification Resolve() => IdentificationHelper.Read<T>();
    }
}
