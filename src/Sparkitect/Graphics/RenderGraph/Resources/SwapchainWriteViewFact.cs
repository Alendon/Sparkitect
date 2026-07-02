using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Builds the swapchain write view by resolving the sub-declared leaf and composing a hook-contributing <see cref="SwapchainWriteView"/> over it.</summary>
[FactRegistry.Register("swapchain_write_view")]
public sealed partial record SwapchainWriteViewFact
    : DeclaredFact<SwapchainWriteView>, IHasIdentification
{
    /// <summary>Structural ref to the swapchain leaf the view targets; flowed in by the description at Declare.</summary>
    public ResourceRef<ImageResource> Leaf { get; init; }

    /// <inheritdoc/>
    public SwapchainWriteView CreateInstance(IInstanceContext ctx) =>
        new(ctx.Resolve(Leaf));

    /// <summary>The composite owns no disposable; the sub-declared leaf's fact releases the backing.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
