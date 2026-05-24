using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Engine-internal contract for a render graph. Single API: callers ask for a typed handler
/// (e.g. <see cref="IExternalResourceHandler"/>) and get back the implementation if the graph
/// exposes one, or <c>null</c> otherwise. New capabilities are exposed as new handler types,
/// never as new members on this interface.
/// </summary>
[PublicAPI]
public interface IRenderGraph
{
    THandler? GetHandler<THandler>() where THandler : class;
}
