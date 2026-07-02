using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns an image view describing how a shader interprets a subresource range of a <see cref="VkImage"/>.</summary>
[PublicAPI]
public class VkImageView : VulkanObject
{
    /// <summary>Wraps an existing <see cref="ImageView"/> handle, recording the source image and the subresource range it targets.</summary>
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

    /// <summary>The underlying Silk.NET <see cref="ImageView"/> handle.</summary>
    public ImageView Handle { get; }

    /// <summary>The image this view reads from.</summary>
    public VkImage Image { get; }

    /// <summary>The format the view reinterprets the image data as.</summary>
    public Format Format { get; }

    /// <summary>Whether the view addresses the image as 1D/2D/3D/cube/array.</summary>
    public ImageViewType ViewType { get; }

    /// <summary>The aspects (color, depth, stencil) the view exposes.</summary>
    public ImageAspectFlags AspectMask { get; }

    /// <summary>The first mip level the view covers.</summary>
    public uint BaseMipLevel { get; }

    /// <summary>The number of mip levels the view covers.</summary>
    public uint MipLevelCount { get; }

    /// <summary>The first array layer the view covers.</summary>
    public uint BaseArrayLayer { get; }

    /// <summary>The number of array layers the view covers.</summary>
    public uint ArrayLayerCount { get; }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyImageView(Device, Handle, AllocationCallbacks);
    }
}
