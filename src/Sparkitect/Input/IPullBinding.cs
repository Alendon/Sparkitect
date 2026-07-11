using JetBrains.Annotations;

namespace Sparkitect.Input;

/// <summary>
/// A live pull binding for one resolved action. <see cref="Read"/> returns the same
/// already-processed <see cref="ActionResult{T}"/> the implementation produced this frame,
/// preserving <c>NoValue</c> — it never resamples or reevaluates the underlying provider.
/// Disposing is idempotent; reading after dispose fails loud.
/// </summary>
/// <typeparam name="T">The action's result type.</typeparam>
[PublicAPI]
public interface IPullBinding<T> : IDisposable
{
    /// <summary>
    /// Reads the current already-processed result. Throws if this binding has been disposed.
    /// </summary>
    ActionResult<T> Read();
}
