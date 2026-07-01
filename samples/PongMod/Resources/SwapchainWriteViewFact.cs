using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace PongMod.Resources;

/// <summary>
/// Builds the copy pass's swapchain write view: resolves the sub-declared swapchain leaf dependency-first
/// (the same cached instance the present target resolves) and composes a hook-contributing
/// <see cref="SwapchainWriteView"/> over it. Cleanup is <see cref="CleanupStrategy.None"/>: the composite
/// owns no disposable object — the leaf's own fact releases the swapchain-owned backing.
/// </summary>
[FactRegistry.Register("pong_swapchain_write_view")]
public sealed partial record SwapchainWriteViewFact
    : DeclaredFact<SwapchainWriteView>, IHasIdentification
{
    /// <summary>The sub-declared swapchain leaf, set by the description at Declare.</summary>
    public ResourceRef<ImageResource> Leaf { get; init; }

    /// <inheritdoc/>
    public SwapchainWriteView CreateInstance(IInstanceContext ctx) =>
        new(ctx.Resolve(Leaf));

    /// <summary>The composite owns no disposable; the sub-declared leaf's fact releases the backing.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
