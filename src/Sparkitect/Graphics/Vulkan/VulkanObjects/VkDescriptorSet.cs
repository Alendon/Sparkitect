using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a descriptor set allocated from a pool and writes resource bindings into it.</summary>
[PublicAPI]
public class VkDescriptorSet : VulkanObject
{
    /// <summary>Wraps a descriptor set allocated from <paramref name="parentPool"/>.</summary>
    public VkDescriptorSet(DescriptorSet handle, IVulkanContext vulkanContext, VkDescriptorPool parentPool, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
        ParentPool = parentPool;
    }

    /// <summary>The underlying Silk.NET <see cref="DescriptorSet"/> handle.</summary>
    public DescriptorSet Handle { get; }

    /// <summary>The pool this set was allocated from and is freed with.</summary>
    public VkDescriptorPool ParentPool { get; }

    /// <summary>No-op: descriptor sets are implicitly freed when their pool is destroyed.</summary>
    public override void Destroy()
    {
    }

    /// <summary>Binds an image view as a storage image at <paramref name="binding"/>.</summary>
    public unsafe void WriteStorageImage(uint binding, VkImageView view, ImageLayout layout)
    {
        var imageInfo = new DescriptorImageInfo
        {
            ImageView = view.Handle,
            ImageLayout = layout,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = Handle,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            PImageInfo = &imageInfo,
        };
        Vk.UpdateDescriptorSets(Device, 1, in write, 0, null);
    }

    /// <summary>Binds a buffer range as a storage buffer at <paramref name="binding"/>; <paramref name="range"/> defaults to the whole buffer.</summary>
    public unsafe void WriteStorageBuffer(uint binding, VkBuffer buffer, ulong offset = 0, ulong? range = null)
    {
        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = buffer.Handle,
            Offset = offset,
            Range = range ?? Vk.WholeSize,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = Handle,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &bufferInfo,
        };
        Vk.UpdateDescriptorSets(Device, 1, in write, 0, null);
    }

    /// <summary>Binds a sampled image plus sampler as a combined image sampler at <paramref name="binding"/>.</summary>
    public unsafe void WriteCombinedImageSampler(uint binding, VkImageView view, VkSampler sampler, ImageLayout layout)
    {
        var imageInfo = new DescriptorImageInfo
        {
            Sampler = sampler.Handle,
            ImageView = view.Handle,
            ImageLayout = layout,
        };
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = Handle,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            PImageInfo = &imageInfo,
        };
        Vk.UpdateDescriptorSets(Device, 1, in write, 0, null);
    }
}
