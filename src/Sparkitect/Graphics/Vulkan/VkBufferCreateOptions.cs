using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public sealed record VkBufferCreateOptions(
    ulong Size,
    BufferUsageFlags Usage,
    SharingMode SharingMode = SharingMode.Exclusive);
