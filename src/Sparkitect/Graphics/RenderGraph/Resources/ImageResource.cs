using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public sealed class ImageResource
{
    public VkImage Backing { get; }

    /// <summary>The image extent.</summary>
    public Extent2D Extent { get; }

    /// <summary>The image format.</summary>
    public Format Format { get; }

    /// <summary>
    /// The backing's current layout. Public and mutable: callers issuing their own barriers write it
    /// directly to keep custom synchronization coherent; <see cref="TransitionTo"/> writes it for the
    /// common path.
    /// </summary>
    public ImageLayout CurrentLayout { get; set; }

    /// <summary>The backing's current access mask, written alongside <see cref="CurrentLayout"/>.</summary>
    public AccessFlags CurrentAccess { get; set; }
    
    /// <summary>
    /// Constructs a leaf scoped to one acquired swapchain backing with its initial tracked state.
    /// </summary>
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
    /// Emits a layout transition for the backing and writes the resulting layout / access back into the
    /// leaf's carried state. No-op if the backing is already in <paramref name="newLayout"/>. The source
    /// stage is derived from the current access (top-of-pipe when no prior access).
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

    private static PipelineStageFlags DeriveStage(AccessFlags access) => access switch
    {
        AccessFlags.TransferWriteBit => PipelineStageFlags.TransferBit,
        AccessFlags.ShaderWriteBit => PipelineStageFlags.ComputeShaderBit,
        AccessFlags.ColorAttachmentWriteBit => PipelineStageFlags.ColorAttachmentOutputBit,
        _ => PipelineStageFlags.TopOfPipeBit,
    };
}
