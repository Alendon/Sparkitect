using JetBrains.Annotations;

namespace Sparkitect.WindowInput.Bindings;

/// <summary>
/// Bulk-fill contract for one channel vocabulary (e.g. keyboard keys, mouse buttons, gamepad
/// axes). The implementation hands the provider its list of channel values referenced this frame
/// and receives the corresponding raw sampled results back — one interface call per channel per
/// frame, never one call per binding instance. Implementations are the natural home for
/// device-specific concerns (focus-lost behavior, dead-zone raw scaling); a provider is the only
/// party that names a concrete device vocabulary.
/// </summary>
/// <typeparam name="TValue">The channel's value vocabulary (e.g. a key enum).</typeparam>
/// <typeparam name="TRaw">The raw sampled result shape for one value (e.g. a pressed-state bool).</typeparam>
[PublicAPI]
public interface IInputSourceProvider<TValue, TRaw>
{
    /// <summary>
    /// Bulk-samples <paramref name="values"/> into <paramref name="results"/> (same length, same
    /// order) for the current frame.
    /// </summary>
    /// <param name="values">The channel values this frame's binding instances reference.</param>
    /// <param name="results">Receives one raw sample per entry in <paramref name="values"/>.</param>
    void Sample(ReadOnlySpan<TValue> values, Span<TRaw> results);
}
