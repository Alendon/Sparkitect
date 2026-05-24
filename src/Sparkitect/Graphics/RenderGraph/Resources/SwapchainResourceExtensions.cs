using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Hand-written publish surface for <see cref="SwapchainResource"/>. Forwards through
/// <see cref="IRenderGraph.GetHandler{THandler}"/> to the graph's
/// <see cref="IExternalResourceHandler"/>; throws if the graph does not expose one.
/// </summary>
[PublicAPI]
public static class SwapchainResourceExtensions
{
    extension(SwapchainResource swapchainResource)
    {
        public void Apply(IRenderGraph renderGraph)
        {
            var handler = renderGraph.GetHandler<IExternalResourceHandler>()
                ?? throw new InvalidOperationException(
                    "SwapchainResource.Apply: the render graph does not expose an IExternalResourceHandler.");
            handler.Publish(swapchainResource);
        }
    }
}
