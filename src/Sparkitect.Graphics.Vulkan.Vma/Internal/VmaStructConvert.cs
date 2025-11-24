using Silk.NET.Vulkan;
using Vortice.Vulkan;
using VkBufferCreateInfo = Vortice.Vulkan.VkBufferCreateInfo;
using VkImageCreateInfo = Vortice.Vulkan.VkImageCreateInfo;
using VkSharingMode = Vortice.Vulkan.VkSharingMode;
using VmaAllocationCreateInfo = Vortice.Vulkan.VmaAllocationCreateInfo;

namespace Sparkitect.Graphics.Vulkan.Vma.Internal;

internal static class VmaStructConvert
{
    internal static Vortice.Vulkan.VmaAllocationCreateInfo ToVortice(in Vma.VmaAllocationCreateInfo info, VmaPool? pool)
    {
        return new Vortice.Vulkan.VmaAllocationCreateInfo
        {
            flags = (Vortice.Vulkan.VmaAllocationCreateFlags)info.Flags,
            usage = (Vortice.Vulkan.VmaMemoryUsage)info.Usage,
            requiredFlags = (VkMemoryPropertyFlags)info.RequiredFlags,
            preferredFlags = (VkMemoryPropertyFlags)info.PreferredFlags,
            memoryTypeBits = info.MemoryTypeBits,
            pool = pool is not null ? new Vortice.Vulkan.VmaPool(pool.Handle) : default,
            priority = info.Priority,
        };
    }

    internal static unsafe VkBufferCreateInfo ToVortice(in BufferCreateInfo info)
    {
        return new VkBufferCreateInfo
        {
            flags = (VkBufferCreateFlags)info.Flags,
            size = info.Size,
            usage = (VkBufferUsageFlags)info.Usage,
            sharingMode = (VkSharingMode)info.SharingMode,
            queueFamilyIndexCount = info.QueueFamilyIndexCount,
            pQueueFamilyIndices = info.PQueueFamilyIndices,
        };
    }

    internal static unsafe VkImageCreateInfo ToVortice(in ImageCreateInfo info)
    {
        return new VkImageCreateInfo
        {
            flags = (VkImageCreateFlags)info.Flags,
            imageType = (VkImageType)info.ImageType,
            format = (VkFormat)info.Format,
            extent = new VkExtent3D { width = info.Extent.Width, height = info.Extent.Height, depth = info.Extent.Depth },
            mipLevels = info.MipLevels,
            arrayLayers = info.ArrayLayers,
            samples = (VkSampleCountFlags)info.Samples,
            tiling = (VkImageTiling)info.Tiling,
            usage = (VkImageUsageFlags)info.Usage,
            sharingMode = (VkSharingMode)info.SharingMode,
            queueFamilyIndexCount = info.QueueFamilyIndexCount,
            pQueueFamilyIndices = info.PQueueFamilyIndices,
            initialLayout = (VkImageLayout)info.InitialLayout,
        };
    }

    internal static unsafe VmaAllocationInfo ToPublic(in Vortice.Vulkan.VmaAllocationInfo info)
    {
        return new VmaAllocationInfo(
            memoryType: info.memoryType,
            deviceMemory: info.deviceMemory.ToSilk(),
            offset: info.offset,
            size: info.size,
            mappedData: (IntPtr)info.pMappedData
        );
    }
}
