using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>Parameters for creating a <see cref="VulkanObjects.VkBuffer"/>.</summary>
/// <param name="Size">Buffer size in bytes.</param>
/// <param name="Usage">How the buffer will be used.</param>
/// <param name="SharingMode">Whether the buffer is shared across queue families.</param>
[PublicAPI]
public sealed record VkBufferCreateOptions(
    ulong Size,
    BufferUsageFlags Usage,
    SharingMode SharingMode = SharingMode.Exclusive);
