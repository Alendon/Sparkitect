using JetBrains.Annotations;

namespace Sparkitect.Input;

/// <summary>
/// Bulk-fill contract for one channel vocabulary (e.g. keyboard keys, mouse buttons, gamepad
/// axes). Input hands the provider its deduped list of referenced channel values for the frame
/// and receives the corresponding raw sampled results back — one interface call per channel per
/// frame, never one call per binding instance. Implementations are the natural home for
/// device-specific concerns (focus-lost behavior, dead-zone raw scaling); a provider is the only
/// party that names a concrete device vocabulary — Input core never does.
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
    /// <param name="values">The deduped channel values Input needs sampled this frame.</param>
    /// <param name="results">Receives one raw sample per entry in <paramref name="values"/>.</param>
    void Sample(ReadOnlySpan<TValue> values, Span<TRaw> results);
}
