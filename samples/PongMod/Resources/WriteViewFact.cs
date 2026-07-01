using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace PongMod.Resources;

/// <summary>
/// Builds the compute write view's live instance: resolves the VMA-transient leaf from the image manager
/// (N=1 stable), creates one <see cref="VkImageView"/> over it, and wraps both in a
/// <see cref="StorageWriteView"/>. The <see cref="ExtentIntent"/> + <see cref="Format"/> flow in from the
/// description via a record <c>with</c> (the DI keyed factory constructs the fact without per-declaration
/// data).
/// </summary>
[FactRegistry.Register("pong_write_view")]
public sealed partial record WriteViewFact(IImageManager? Provider)
    : DeclaredFact<StorageWriteView>, IHasIdentification
{
    /// <summary>The symbolic size of the transient target, set by the description at Declare.</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The transient target's format, set by the description at Declare.</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public StorageWriteView CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "WriteViewFact.CreateInstance: no image backing provider was injected. The graph-local " +
                "IImageManager must be resolvable when the fact factory builds this fact.");

        var leaf = Provider.ResolveTransientLeaf(Extent, Format);

        var viewResult = leaf.Backing.CreateView(ImageAspectFlags.ColorBit);
        if (viewResult is not Result<VkImageView, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                "WriteViewFact.CreateInstance: failed to create the storage image view " +
                $"({((Result<VkImageView, VkApiResult>.Error)viewResult).Value}).");

        return new StorageWriteView(leaf, ok.Value);
    }

    /// <summary>The view owns its <see cref="VkImageView"/> and disposes it directly at teardown.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.Dispose;
}
