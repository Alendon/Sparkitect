using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Engine-internal contract for a render graph. Capabilities are exposed as typed handlers, never as new
/// members on this interface; callers ask for a handler and get the implementation or <c>null</c>.
/// </summary>
[PublicAPI]
public interface IRenderGraph
{
    THandler? GetHandler<THandler>() where THandler : class;
}
