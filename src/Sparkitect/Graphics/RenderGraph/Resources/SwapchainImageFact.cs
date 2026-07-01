using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Resolves a swapchain-backed image leaf through the graph-local image manager. Cleanup is
/// <see cref="CleanupStrategy.Release"/>: the swapchain owns the backing, not this leaf.
/// </summary>
[FactRegistry.Register("swapchain_image")]
public sealed partial record SwapchainImageFact(IImageManager? Provider)
    : DeclaredFact<ImageResource>, IHasIdentification
{
    /// <inheritdoc/>
    public ImageResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "SwapchainImageFact.CreateInstance: no image backing provider was injected. The graph-local " +
                "IImageManager must be resolvable when the fact factory builds this fact.");

        return Provider.ResolveSwapchainLeaf();
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
