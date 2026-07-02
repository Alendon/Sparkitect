using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Engine-internal contract for a render graph. Capabilities are exposed as typed handlers, never as new
/// members on this interface; callers ask for a handler and get the implementation or <c>null</c>.
/// </summary>
[PublicAPI]
public interface IRenderGraph
{
    /// <summary>Returns the graph's implementation of <typeparamref name="THandler"/>, or <c>null</c> when the graph does not expose that capability.</summary>
    THandler? GetHandler<THandler>() where THandler : class;
}
