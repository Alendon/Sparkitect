using Silk.NET.Vulkan;
using Sparkitect.Utils;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkCommandBuffer : VulkanObject
{
    public VkCommandBuffer(CommandBuffer commandBuffer, IVulkanContext vulkanContext, VkCommandPool pool,
        CallerContext callerContext = default) : base(vulkanContext, callerContext)
    {
        Handle = commandBuffer;
        ParentPool = pool;
    }

    public CommandBuffer Handle { get; }
    public VkCommandPool ParentPool { get; }

    public VkApiResult Reset(CommandBufferResetFlags flags = 0)
        => Vk.ResetCommandBuffer(Handle, flags);

    public VkApiResult Begin(CommandBufferUsageFlags flags = 0)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = flags
        };
        return Vk.BeginCommandBuffer(Handle, in beginInfo);
    }

    public VkApiResult End()
        => Vk.EndCommandBuffer(Handle);

    public void ImageBarrier(
        VkImage image,
        ImageLayout oldLayout, ImageLayout newLayout,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        AccessFlags srcAccess, AccessFlags dstAccess)
    {
        ImageBarrier(image, oldLayout, newLayout, srcStage, dstStage, srcAccess, dstAccess,
            new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0, LevelCount = 1,
                BaseArrayLayer = 0, LayerCount = 1,
            });
    }

    public unsafe void ImageBarrier(
        VkImage image,
        ImageLayout oldLayout, ImageLayout newLayout,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        AccessFlags srcAccess, AccessFlags dstAccess,
        ImageSubresourceRange subresourceRange)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            Image = image.Handle,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            SubresourceRange = subresourceRange,
        };
        Vk.CmdPipelineBarrier(
            Handle, srcStage, dstStage, 0,
            0, null, 0, null, 1, &barrier);
    }

    public void BindPipeline(PipelineBindPoint bindPoint, VkPipeline pipeline)
        => Vk.CmdBindPipeline(Handle, bindPoint, pipeline.Handle);

    public unsafe void BindDescriptorSets(
        PipelineBindPoint bindPoint,
        VkPipelineLayout layout,
        uint firstSet,
        VkDescriptorSet descriptorSet)
    {
        var handle = descriptorSet.Handle;
        Vk.CmdBindDescriptorSets(Handle, bindPoint, layout.Handle, firstSet,
            1, &handle, 0, null);
    }

    public unsafe void BindDescriptorSets(
        PipelineBindPoint bindPoint,
        VkPipelineLayout layout,
        uint firstSet,
        ReadOnlySpan<VkDescriptorSet> descriptorSets)
    {
        var handles = descriptorSets.Length < 16
            ? stackalloc DescriptorSet[descriptorSets.Length]
            : new DescriptorSet[descriptorSets.Length];
        for (var i = 0; i < descriptorSets.Length; i++)
            handles[i] = descriptorSets[i].Handle;

        fixed (DescriptorSet* ptr = handles)
        {
            Vk.CmdBindDescriptorSets(Handle, bindPoint, layout.Handle, firstSet,
                (uint)descriptorSets.Length, ptr, 0, null);
        }
    }

    public unsafe void PushConstants<T>(
        VkPipelineLayout layout,
        ShaderStageFlags stageFlags,
        uint offset,
        in T data) where T : unmanaged
    {
        fixed (T* ptr = &data)
        {
            Vk.CmdPushConstants(Handle, layout.Handle, stageFlags, offset, (uint)sizeof(T), ptr);
        }
    }

    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        => Vk.CmdDispatch(Handle, groupCountX, groupCountY, groupCountZ);

    public void ClearColorImage(VkImage image, ImageLayout imageLayout, in ClearColorValue color)
    {
        var range = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            BaseMipLevel = 0, LevelCount = 1,
            BaseArrayLayer = 0, LayerCount = 1,
        };
        Vk.CmdClearColorImage(Handle, image.Handle, imageLayout, in color, 1, in range);
    }

    public void ClearColorImage(
        VkImage image, ImageLayout imageLayout, in ClearColorValue color,
        in ImageSubresourceRange range)
    {
        Vk.CmdClearColorImage(Handle, image.Handle, imageLayout, in color, 1, in range);
    }

    public void BlitImage(
        VkImage srcImage, ImageLayout srcLayout,
        VkImage dstImage, ImageLayout dstLayout,
        in ImageBlit region, Filter filter)
    {
        Vk.CmdBlitImage(Handle, srcImage.Handle, srcLayout, dstImage.Handle, dstLayout,
            1, in region, filter);
    }

    /// <summary>
    /// Blits the full extent of <paramref name="src"/> to the full extent of
    /// <paramref name="dst"/>. Derives <c>SrcOffsets</c> and <c>DstOffsets</c>
    /// from each image's <see cref="VkImage.Extent"/>.
    /// </summary>
    public void BlitFullExtent(
        VkImage src, ImageLayout srcLayout,
        VkImage dst, ImageLayout dstLayout,
        Filter filter)
    {
        var blit = new ImageBlit
        {
            SrcSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            DstSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
        };
        blit.SrcOffsets[0] = default;
        blit.SrcOffsets[1] = new Offset3D(
            (int)src.Extent.Width, (int)src.Extent.Height, (int)src.Extent.Depth);
        blit.DstOffsets[0] = default;
        blit.DstOffsets[1] = new Offset3D(
            (int)dst.Extent.Width, (int)dst.Extent.Height, (int)dst.Extent.Depth);

        BlitImage(src, srcLayout, dst, dstLayout, in blit, filter);
    }

    public override void Destroy()
    {
        ParentPool.FreeCommandBuffers(this);
    }
}
