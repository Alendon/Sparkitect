using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace PongMod.Resources;

/// <summary>
/// Builds the copy pass's swapchain write view: ctor-injects the graph-local image manager, resolves the
/// swapchain leaf (index-selected per frame — Pitfall 3), and wraps it in a <see cref="SwapchainWriteView"/>
/// that contributes the transfer-dst and present transitions as lifecycle hooks. Cleanup is
/// <see cref="CleanupStrategy.Release"/>: the swapchain backing is owned by the swapchain, not this leaf.
/// </summary>
[FactRegistry.Register("pong_swapchain_write_view")]
public sealed partial record SwapchainWriteViewFact(IImageManager? Provider)
    : DeclaredFact<SwapchainWriteView>, IHasIdentification
{
    /// <inheritdoc/>
    public SwapchainWriteView CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "SwapchainWriteViewFact.CreateInstance: no image backing provider was injected. The " +
                "graph-local IImageManager must be resolvable when the fact factory builds this fact.");

        return new SwapchainWriteView(Provider.ResolveSwapchainLeaf());
    }

    /// <summary>The swapchain backing is owned by the swapchain, not disposed by this leaf.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
