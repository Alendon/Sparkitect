using JetBrains.Annotations;
using Sparkitect.Input;

namespace Sparkitect.WindowInput.Bindings;

/// <summary>
/// The open modding seam: a binding type owns the entire sliver connecting physical
/// source values to an action's result — which source values it references, its
/// settings-backing shape (only it knows how many/which keys it needs), how it refreshes its own
/// raw channel slots each frame, and how it composes those slots into an
/// <see cref="ActionResult{T}"/>. Input core never learns channels, keys, or composite shapes;
/// an implementer is free to read whatever source vocabulary it needs through its own
/// <see cref="IInputSourceProvider{TValue,TRaw}"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete binding type implementing this interface. Lets the
/// per-frame processing loop batch-sample/evaluate a whole type-group through one
/// generic-dispatch entry per closed instantiation, devirtualized by the JIT — not one virtual
/// call per instance.</typeparam>
/// <typeparam name="TResult">The result type this binding type produces (matches the declaring
/// action's result type).</typeparam>
/// <remarks>
/// Sampled and evaluated BUNCHED by concrete binding type: the per-frame processing loop calls
/// <see cref="Sample"/> then <see cref="Evaluate"/> once per distinct concrete binding type, over
/// that type's whole contiguous live-instance span.
/// </remarks>
[PublicAPI]
public interface IBindingType<TSelf, TResult> where TSelf : IBindingType<TSelf, TResult>
{
    /// <summary>
    /// Refreshes each entry's raw channel slots from <paramref name="sampling"/> in place, ahead
    /// of <see cref="Evaluate"/> for the same frame — the binding type owns its own per-frame
    /// raw-slot refresh; Input never samples on a binding's behalf.
    /// </summary>
    /// <param name="instances">This binding type's contiguous live-instance span for the current
    /// frame.</param>
    /// <param name="sampling">Channel-agnostic bulk lookup routing to the registered provider for
    /// each referenced channel value; a providerless channel leaves its slots untouched.</param>
    static abstract void Sample(Span<TSelf> instances, IInputSourceSampling sampling);

    /// <summary>
    /// Composes one <see cref="ActionResult{T}"/> per entry in <paramref name="instances"/>,
    /// writing into the matching slot of <paramref name="results"/> (same length, same order).
    /// </summary>
    /// <param name="instances">This binding type's contiguous live-instance span for the current
    /// frame.</param>
    /// <param name="results">Receives one composed result per entry in
    /// <paramref name="instances"/>.</param>
    static abstract void Evaluate(ReadOnlySpan<TSelf> instances, Span<ActionResult<TResult>> results);
}
