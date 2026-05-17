using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkDescriptorSet : VulkanObject
{
    public VkDescriptorSet(DescriptorSet handle, IVulkanContext vulkanContext, VkDescriptorPool parentPool, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
        ParentPool = parentPool;
    }

    public DescriptorSet Handle { get; }
    public VkDescriptorPool ParentPool { get; }

    public override void Destroy()
    {
        // DescriptorSets are implicitly freed when their pool is destroyed.
        // Do not call vkFreeDescriptorSets here - pool handles cleanup.
    }

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
