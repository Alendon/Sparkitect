using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Builds the compute write view: resolves the sub-declared transient leaf, creates a <see cref="VkImageView"/> over it, and wraps both in a <see cref="StorageWriteView"/>.</summary>
[FactRegistry.Register("storage_write_view")]
public sealed partial record StorageWriteViewFact : DeclaredFact<StorageWriteView>, IHasIdentification
{
    /// <summary>Structural ref to the shared transient leaf the view writes; flowed in by the description at Declare.</summary>
    public ResourceRef<ImageResource> LeafRef { get; init; }

    /// <inheritdoc/>
    public StorageWriteView CreateInstance(IInstanceContext ctx)
    {
        var leaf = ctx.Resolve(LeafRef);

        var viewResult = leaf.Backing.CreateView(ImageAspectFlags.ColorBit);
        if (viewResult is not Result<VkImageView, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                "StorageWriteViewFact.CreateInstance: failed to create the storage image view " +
                $"({((Result<VkImageView, VkApiResult>.Error)viewResult).Value}).");

        return new StorageWriteView(leaf, ok.Value);
    }

    /// <summary>The view owns its <see cref="VkImageView"/> and disposes it directly at teardown.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.Dispose;
}
