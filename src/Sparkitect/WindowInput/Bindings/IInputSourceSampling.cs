using JetBrains.Annotations;

namespace Sparkitect.WindowInput.Bindings;

/// <summary>
/// Channel-agnostic per-frame sampling facade a binding type's
/// <see cref="IBindingType{TSelf,TResult}.Sample"/> step routes through to refresh its raw
/// channel slots. Routes to the registered <see cref="IInputSourceProvider{TValue,TRaw}"/> for
/// <c>TValue</c>; a channel with no registered provider leaves <c>results</c> untouched — a
/// providerless channel is a composition state, never an error (D-20). Public: <see cref="IBindingType{TSelf,TResult}"/>
/// is an open modding seam, so a third-party binding type's <c>Sample</c> implementation must be
/// able to receive and call this parameter type.
/// </summary>
[PublicAPI]
public interface IInputSourceSampling
{
    /// <summary>
    /// Bulk-samples <paramref name="values"/> into <paramref name="results"/> through the
    /// provider registered for <typeparamref name="TValue"/>, or leaves
    /// <paramref name="results"/> untouched when no provider is registered for that channel.
    /// </summary>
    /// <typeparam name="TValue">The channel's value vocabulary.</typeparam>
    /// <typeparam name="TRaw">The raw sampled result shape for one value.</typeparam>
    /// <param name="values">The channel values to sample.</param>
    /// <param name="results">Receives one raw sample per entry in <paramref name="values"/>.</param>
    void Sample<TValue, TRaw>(ReadOnlySpan<TValue> values, Span<TRaw> results);
}
