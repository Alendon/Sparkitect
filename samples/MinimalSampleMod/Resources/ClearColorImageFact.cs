using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace MinimalSampleMod.Resources;

/// <summary>
/// Mod-owned fact for the clear-color image. It resolves the shared swapchain leaf the description
/// sub-declared and wraps it in a <see cref="ClearColorImageView"/> that contributes the transfer-dst
/// and present transitions as lifecycle hooks. Cleanup is <see cref="CleanupStrategy.Release"/>: the
/// swapchain backing is owned by the swapchain, not this leaf.
/// </summary>
[FactRegistry.Register("clear_color_image")]
public sealed partial record ClearColorImageFact
    : DeclaredFact<ImageResource>, IHasIdentification
{
    /// <summary>The shared swapchain leaf sub-declared by the description, resolved dependency-first.</summary>
    public ResourceRef<ImageResource> Leaf { get; init; }

    /// <inheritdoc/>
    public ImageResource CreateInstance(IInstanceContext ctx) =>
        new ClearColorImageView(ctx.Resolve(Leaf));

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
