using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace PongMod.Resources;

/// <summary>Builds the compute write view: resolves the sub-declared transient leaf, creates a <see cref="VkImageView"/> over it, and wraps both in a <see cref="StorageWriteView"/>.</summary>
[FactRegistry.Register("pong_write_view")]
public sealed partial record WriteViewFact : DeclaredFact<StorageWriteView>, IHasIdentification
{
    public ResourceRef<ImageResource> LeafRef { get; init; }

    /// <inheritdoc/>
    public StorageWriteView CreateInstance(IInstanceContext ctx)
    {
        var leaf = ctx.Resolve(LeafRef);

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
