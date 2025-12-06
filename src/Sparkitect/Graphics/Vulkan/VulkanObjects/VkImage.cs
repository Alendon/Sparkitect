using JetBrains.Annotations;
using Silk.NET.Vulkan;

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
        IVulkanContext context) : base(context)
    {
        Handle = handle;
        Format = format;
        Extent = extent;
        MipLevels = mipLevels;
        ArrayLayers = arrayLayers;
        ImageType = imageType;
    }

    public Image Handle { get; }
    public Format Format { get; }
    public Extent3D Extent { get; }
    public uint MipLevels { get; }
    public uint ArrayLayers { get; }
    public ImageType ImageType { get; }

    /// <summary>
    /// Creates an image view for this image with inferred defaults.
    /// </summary>
    /// <param name="aspectMask">Aspect mask. Defaults to Color for color formats, Depth/Stencil for depth formats.</param>
    public VkResult<VkImageView> CreateView(ImageAspectFlags? aspectMask = null)
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
            if (result != Result.Success)
                return VkResult<VkImageView>._Error(result);

            return VkResult<VkImageView>._Success(new VkImageView(
                imageView, this, Format, viewType, aspect,
                0, MipLevels, 0, ArrayLayers, VulkanContext));
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

    public override unsafe void Destroy()
    {
        Vk.DestroyImage(Device, Handle, AllocationCallbacks);
    }
}
