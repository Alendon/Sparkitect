using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkImageView : VulkanObject
{
    public VkImageView(
        ImageView handle,
        VkImage image,
        Format format,
        ImageViewType viewType,
        ImageAspectFlags aspectMask,
        uint baseMipLevel,
        uint mipLevelCount,
        uint baseArrayLayer,
        uint arrayLayerCount,
        IVulkanContext context) : base(context)
    {
        Handle = handle;
        Image = image;
        Format = format;
        ViewType = viewType;
        AspectMask = aspectMask;
        BaseMipLevel = baseMipLevel;
        MipLevelCount = mipLevelCount;
        BaseArrayLayer = baseArrayLayer;
        ArrayLayerCount = arrayLayerCount;
    }

    public ImageView Handle { get; }
    public VkImage Image { get; }
    public Format Format { get; }
    public ImageViewType ViewType { get; }
    public ImageAspectFlags AspectMask { get; }
    public uint BaseMipLevel { get; }
    public uint MipLevelCount { get; }
    public uint BaseArrayLayer { get; }
    public uint ArrayLayerCount { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyImageView(Device, Handle, AllocationCallbacks);
    }
}
