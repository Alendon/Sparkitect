using JetBrains.Annotations;
using Sparkitect.Input;

namespace Sparkitect.Input.Bindings;

/// <summary>
/// The open modding seam (D-16): a binding type owns the entire sliver connecting physical
/// source values to an action's result — which source values it references, its
/// settings-backing shape (only it knows how many/which keys it needs), and how to compose
/// sampled raw channel slots into an <see cref="ActionResult{T}"/>. Input core never learns
/// channels, keys, or composite shapes; an implementer is free to read whatever source
/// vocabulary it needs through its own <see cref="IInputSourceProvider{TValue,TRaw}"/>.
/// </summary>
/// <typeparam name="TSelf">The concrete binding type implementing this interface. Lets the
/// per-frame snapshot loop batch-evaluate a whole type-group through one generic-dispatch entry
/// per closed instantiation, devirtualized by the JIT — not one virtual call per instance.</typeparam>
/// <typeparam name="TResult">The result type this binding type produces (matches the declaring
/// action's result type).</typeparam>
/// <remarks>
/// Evaluated BUNCHED by concrete binding type (D-18): the per-frame snapshot loop calls
/// <see cref="Evaluate"/> once per distinct concrete binding type, over that type's whole
/// contiguous live-instance span. Dirty (re-resolution) processing happens before this runs, so
/// implementers can assume their own settings-derived state is current for the frame.
/// </remarks>
[PublicAPI]
public interface IBindingType<TSelf, TResult> where TSelf : IBindingType<TSelf, TResult>
{
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
