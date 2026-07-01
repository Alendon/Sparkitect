using Silk.NET.Vulkan;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Resolves a VMA-transient image leaf through the graph-local image manager and returns it as a plain
/// <see cref="ImageResource"/> a composite can sub-declare as its shared cross-pass target backing. The
/// <see cref="ExtentIntent"/> + <see cref="Format"/> flow in from the description via a record
/// <c>with</c>. Cleanup is <see cref="CleanupStrategy.Release"/>: the image manager owns the VMA backing.
/// </summary>
[FactRegistry.Register("transient_image")]
public sealed partial record TransientImageFact(IImageManager? Provider)
    : DeclaredFact<ImageResource>, IHasIdentification
{
    /// <summary>The symbolic size of the transient leaf, set by the description at Declare.</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The transient leaf's format, set by the description at Declare.</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public ImageResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "TransientImageFact.CreateInstance: no image backing provider was injected. The graph-local " +
                "IImageManager must be resolvable when the fact factory builds this fact.");

        return Provider.ResolveTransientLeaf(Extent, Format);
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
