using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

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
