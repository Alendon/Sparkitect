using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkImage : VulkanObject
{
    public VkImage(
        Image handle,
        Format format,
        Extent3D extent,
        uint mipLevels,
        uint arrayLayers,
        ImageType imageType,
        ImageUsageFlags usage,
        ImageBacking backing,
        IVulkanContext context,
        CallerContext callerContext = default) : base(context, callerContext)
    {
        Handle = handle;
        Format = format;
        Extent = extent;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        ImageType = imageType;
        Usage = usage;
        Backing = backing;
    }

    public Image Handle { get; }
    public Format Format { get; }
    public Extent3D Extent { get; }
    public uint MipLevels { get; }
    public uint ArrayLayers { get; }
    public ImageType ImageType { get; }
    public ImageUsageFlags Usage { get; }
    public ImageBacking Backing { get; }

    /// <summary>
    /// Creates an image view for this image with inferred defaults.
    /// </summary>
    /// <param name="aspectMask">Aspect mask. Defaults to Color for color formats, Depth/Stencil for depth formats.</param>
    public Result<VkImageView, VkApiResult> CreateView(ImageAspectFlags? aspectMask = null)
    {
        var aspect = aspectMask ?? InferAspectMask(Format);
        var viewType = InferViewType(ImageType, ArrayLayers);

        var subresourceRange = new ImageSubresourceRange
        {
            AspectMask = aspect,
            BaseMipLevel = 0,
            LevelCount = MipLevels,
            BaseArrayLayer = 0,
            LayerCount = ArrayLayers
        };

        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Handle,
            ViewType = viewType,
            Format = Format,
            Components = new ComponentMapping
            {
                R = ComponentSwizzle.Identity,
                G = ComponentSwizzle.Identity,
                B = ComponentSwizzle.Identity,
                A = ComponentSwizzle.Identity
            },
            SubresourceRange = subresourceRange
        };

        unsafe
        {
            var result = Vk.CreateImageView(Device, createInfo, AllocationCallbacks, out var imageView);
            if (result != VkApiResult.Success)
                return result;

            return new VkImageView(
                imageView, this, Format, viewType, aspect,
                0, MipLevels, 0, ArrayLayers, VulkanContext);
        }
    }

    private static ImageAspectFlags InferAspectMask(Format format)
    {
        return format switch
        {
            Format.D16Unorm or Format.D32Sfloat or Format.X8D24UnormPack32
                => ImageAspectFlags.DepthBit,

            Format.S8Uint
                => ImageAspectFlags.StencilBit,

            Format.D16UnormS8Uint or Format.D24UnormS8Uint or Format.D32SfloatS8Uint
                => ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit,

            _ => ImageAspectFlags.ColorBit
        };
    }

    private static ImageViewType InferViewType(ImageType imageType, uint arrayLayers)
    {
        return imageType switch
        {
            ImageType.Type1D => arrayLayers > 1 ? ImageViewType.Type1DArray : ImageViewType.Type1D,
            ImageType.Type2D => arrayLayers > 1 ? ImageViewType.Type2DArray : ImageViewType.Type2D,
            ImageType.Type3D => ImageViewType.Type3D,
            _ => ImageViewType.Type2D
        };
    }

    public override void Destroy()
    {
        switch (Backing)
        {
            case ImageBacking.Swapchain:
                // Swapchain owns the image handle; nothing to destroy here.
                // VkSwapchain.DestroyImageViews calls MarkDisposed() explicitly on its
                // image wrappers, which untracks them; this body is a no-op.
                break;
            case ImageBacking.VmaAllocated v:
                VulkanContext.VmaAllocator.DestroyImage(Handle, v.Value);
                break;
        }
    }
}
