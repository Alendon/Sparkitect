using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkImage"/>.</summary>
/// <param name="Extent">Width, height, and depth in texels.</param>
/// <param name="Format">Pixel format.</param>
/// <param name="Usage">How the image will be used.</param>
/// <param name="MipLevels">Number of mip levels.</param>
/// <param name="ArrayLayers">Number of array layers.</param>
/// <param name="Type">Whether the image is 1D, 2D, or 3D.</param>
/// <param name="Samples">Multisample count.</param>
/// <param name="Tiling">Optimal or linear texel layout.</param>
/// <param name="SharingMode">Whether the image is shared across queue families.</param>
/// <param name="InitialLayout">Layout the image starts in before first use.</param>
[PublicAPI]
public sealed record VkImageCreateOptions(
    Extent3D Extent,
    Format Format,
    ImageUsageFlags Usage,
    uint MipLevels = 1,
    uint ArrayLayers = 1,
    ImageType Type = ImageType.Type2D,
    SampleCountFlags Samples = SampleCountFlags.Count1Bit,
    ImageTiling Tiling = ImageTiling.Optimal,
    SharingMode SharingMode = SharingMode.Exclusive,
    ImageLayout InitialLayout = ImageLayout.Undefined);
