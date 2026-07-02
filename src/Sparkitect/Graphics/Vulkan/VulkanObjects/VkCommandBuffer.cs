using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Records GPU commands (barriers, copies, binds, dispatches) for submission through a <see cref="VkQueue"/>.</summary>
[PublicAPI]
public class VkCommandBuffer : VulkanObject
{
    /// <summary>Wraps a command buffer allocated from <paramref name="pool"/>.</summary>
    public VkCommandBuffer(CommandBuffer commandBuffer, IVulkanContext vulkanContext, VkCommandPool pool,
        CallerContext callerContext = default) : base(vulkanContext, callerContext)
    {
        Handle = commandBuffer;
        ParentPool = pool;
    }

    /// <summary>The underlying Silk.NET <see cref="CommandBuffer"/> handle.</summary>
    public CommandBuffer Handle { get; }

    /// <summary>The pool this buffer was allocated from and is freed with.</summary>
    public VkCommandPool ParentPool { get; }

    /// <summary>Resets the buffer to the initial state, discarding previously recorded commands.</summary>
    public VkApiResult Reset(CommandBufferResetFlags flags = 0)
        => Vk.ResetCommandBuffer(Handle, flags);

    /// <summary>Begins recording. Must be called before recording any commands.</summary>
    public VkApiResult Begin(CommandBufferUsageFlags flags = 0)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = flags
        };
        return Vk.BeginCommandBuffer(Handle, in beginInfo);
    }

    /// <summary>Ends recording. Must be called before the buffer is submitted.</summary>
    public VkApiResult End()
        => Vk.EndCommandBuffer(Handle);

    /// <summary>Records a color-aspect image layout/access barrier over the first mip level and array layer.</summary>
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

    /// <summary>Records an image layout/access barrier over the given <paramref name="subresourceRange"/>.</summary>
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

    /// <summary>Records a buffer-to-buffer copy of <paramref name="size"/> bytes.</summary>
    public unsafe void CopyBuffer(VkBuffer src, VkBuffer dst, ulong size, ulong srcOffset = 0, ulong dstOffset = 0)
    {
        var region = new BufferCopy { SrcOffset = srcOffset, DstOffset = dstOffset, Size = size };
        Vk.CmdCopyBuffer(Handle, src.Handle, dst.Handle, 1, &region);
    }

    /// <summary>Records a buffer-memory barrier over the whole buffer.</summary>
    public unsafe void BufferBarrier(
        VkBuffer buffer,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        AccessFlags srcAccess, AccessFlags dstAccess)
    {
        var barrier = new BufferMemoryBarrier
        {
            SType = StructureType.BufferMemoryBarrier,
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Buffer = buffer.Handle,
            Offset = 0,
            Size = Vk.WholeSize,
        };
        Vk.CmdPipelineBarrier(Handle, srcStage, dstStage, 0, 0, null, 1, &barrier, 0, null);
    }

    /// <summary>Binds a pipeline at the given <paramref name="bindPoint"/> for subsequent draws or dispatches.</summary>
    public void BindPipeline(PipelineBindPoint bindPoint, VkPipeline pipeline)
        => Vk.CmdBindPipeline(Handle, bindPoint, pipeline.Handle);

    /// <summary>Binds a single descriptor set starting at <paramref name="firstSet"/>.</summary>
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

    /// <summary>Binds a contiguous range of descriptor sets starting at <paramref name="firstSet"/>.</summary>
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

    /// <summary>
    /// Pushes descriptor-set writes inline into the command buffer via
    /// <c>VK_KHR_push_descriptor</c>, with no pool or pre-allocated descriptor set.
    /// The <see cref="WriteDescriptorSet.DstSet"/> field of each write is ignored.
    /// </summary>
    public unsafe void PushDescriptorSet(
        PipelineBindPoint bindPoint,
        VkPipelineLayout layout,
        uint firstSet,
        ReadOnlySpan<WriteDescriptorSet> writes)
    {
        var pushDescriptor = VulkanContext.KhrPushDescriptor;
        fixed (WriteDescriptorSet* writesPtr = writes)
        {
            pushDescriptor.CmdPushDescriptorSet(
                Handle, bindPoint, layout.Handle, firstSet,
                (uint)writes.Length, writesPtr);
        }
    }

    /// <summary>Uploads <paramref name="data"/> as push constants at <paramref name="offset"/> for the given shader stages.</summary>
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

    /// <summary>Dispatches a compute workload of the given workgroup counts.</summary>
    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
        => Vk.CmdDispatch(Handle, groupCountX, groupCountY, groupCountZ);

    /// <summary>Clears the whole color aspect of <paramref name="image"/> to <paramref name="color"/>.</summary>
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

    /// <summary>Clears the given subresource <paramref name="range"/> of <paramref name="image"/> to <paramref name="color"/>.</summary>
    public void ClearColorImage(
        VkImage image, ImageLayout imageLayout, in ClearColorValue color,
        in ImageSubresourceRange range)
    {
        Vk.CmdClearColorImage(Handle, image.Handle, imageLayout, in color, 1, in range);
    }

    /// <summary>Records a filtered blit of <paramref name="region"/> from one image to another.</summary>
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

    /// <inheritdoc/>
    public override void Destroy()
    {
        ParentPool.FreeCommandBuffers(this);
    }
}
