using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace MinimalSampleMod.Resources;

/// <summary>
/// Mod-owned fact for the clear-color image. Ctor-injects the graph-local image manager, resolves the
/// swapchain leaf, and wraps it in a <see cref="ClearColorImageView"/> that contributes the transfer-dst
/// and present transitions as lifecycle hooks. Cleanup is <see cref="CleanupStrategy.Release"/>: the
/// swapchain backing is owned by the swapchain, not this leaf.
/// </summary>
[FactRegistry.Register("clear_color_image")]
public sealed partial record ClearColorImageFact(IImageManager? Provider)
    : DeclaredFact<ImageResource>, IHasIdentification
{
    /// <inheritdoc/>
    public ImageResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "ClearColorImageFact.CreateInstance: no image backing provider was injected. The fact " +
                "registry must resolve IImageManager from the per-graph container before CreateInstance runs.");

        return new ClearColorImageView(Provider.ResolveSwapchainLeaf());
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
