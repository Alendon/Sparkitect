using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Push;

/// <summary>
/// The publish door for externally-pushed resource data, reached via
/// <c>graph.GetHandler&lt;IExternalPushHandler&gt;()</c> — distinct from the swapchain external-state
/// handler. The caller publishes a CPU span for a moment; the graph swap-copies it into a graph-owned
/// snapshot keyed by that moment's <see cref="Identification"/>, so the caller keeps mutating its
/// reusable array with no seal handshake.
/// </summary>
[PublicAPI]
public interface IExternalPushHandler
{
    /// <summary>Publishes <paramref name="data"/> for <paramref name="moment"/>: the graph copies the span
    /// into its own per-moment snapshot buffer (swap-copy), leaving the caller's array free to reuse.</summary>
    void Publish<T>(Identification moment, ReadOnlySpan<T> data) where T : unmanaged;
}
