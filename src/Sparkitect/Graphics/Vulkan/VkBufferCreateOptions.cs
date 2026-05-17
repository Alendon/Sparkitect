using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

public sealed record VkBufferCreateOptions(
    ulong Size,
    BufferUsageFlags Usage,
    SharingMode SharingMode = SharingMode.Exclusive);
