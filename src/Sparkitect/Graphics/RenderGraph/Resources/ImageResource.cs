using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


/// <summary>A single image leaf: one backing <see cref="VkImage"/> plus its carried layout/access state, transitioned in place as passes use it.</summary>
[PublicAPI]
public class ImageResource
{
    /// <summary>The backing image this leaf wraps.</summary>
    public VkImage Backing { get; }

    /// <summary>The image extent.</summary>
    public Extent2D Extent { get; }

    /// <summary>The image format.</summary>
    public Format Format { get; }

    /// <summary>The backing's current layout. Mutable: callers issuing their own barriers write it directly to stay coherent.</summary>
    public ImageLayout CurrentLayout { get; set; }

    /// <summary>The backing's current access mask, written alongside <see cref="CurrentLayout"/>.</summary>
    public AccessFlags CurrentAccess { get; set; }
    
    /// <summary>Constructs a leaf over one backing with its initial tracked state.</summary>
    public ImageResource(
        VkImage backing,
        Extent2D extent,
        Format format,
        ImageLayout initialLayout,
        AccessFlags initialAccess)
    {
        Backing = backing;
        Extent = extent;
        Format = format;
        CurrentLayout = initialLayout;
        CurrentAccess = initialAccess;
    }

    /// <summary>
    /// Emits a layout transition and writes the resulting layout/access back into the leaf's carried state.
    /// No-op if already in <paramref name="newLayout"/>; the source stage is derived from the current access.
    /// </summary>
    public void TransitionTo(
        VkCommandBuffer commandBuffer,
        ImageLayout newLayout,
        AccessFlags newAccess,
        PipelineStageFlags dstStage)
    {
        if (CurrentLayout == newLayout) return;

        var srcAccess = CurrentAccess;
        var srcStage = srcAccess == 0 ? PipelineStageFlags.TopOfPipeBit : DeriveStage(srcAccess);

        commandBuffer.ImageBarrier(Backing,
            oldLayout: CurrentLayout,
            newLayout: newLayout,
            srcStage: srcStage,
            dstStage: dstStage,
            srcAccess: srcAccess,
            dstAccess: newAccess);

        CurrentLayout = newLayout;
        CurrentAccess = newAccess;
    }

    private static PipelineStageFlags DeriveStage(AccessFlags access)
    {
        if (access == 0) return PipelineStageFlags.TopOfPipeBit;

        var stage = (PipelineStageFlags)0;
        if ((access & (AccessFlags.TransferReadBit | AccessFlags.TransferWriteBit)) != 0)
            stage |= PipelineStageFlags.TransferBit;
        if ((access & (AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit)) != 0)
            stage |= PipelineStageFlags.ComputeShaderBit;
        if ((access & (AccessFlags.ColorAttachmentReadBit | AccessFlags.ColorAttachmentWriteBit)) != 0)
            stage |= PipelineStageFlags.ColorAttachmentOutputBit;

        return stage == 0 ? PipelineStageFlags.AllCommandsBit : stage;
    }
}
