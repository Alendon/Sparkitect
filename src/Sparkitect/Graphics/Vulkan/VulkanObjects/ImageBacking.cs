using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.Vma;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Discriminates how a <see cref="VkImage"/>'s native handle is backed: either
/// owned by a swapchain (no per-image destruction) or allocated through VMA.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record ImageBacking
{
    /// <summary>The handle is owned by a swapchain and is not destroyed with the image.</summary>
    public sealed partial record Swapchain : ImageBacking;

    /// <summary>The handle is backed by a VMA allocation that the image destroys on disposal.</summary>
    /// <param name="Value">The VMA allocation backing the image.</param>
    public sealed partial record VmaAllocated(VmaAllocation Value) : ImageBacking;

    /// <summary>Wraps a VMA allocation as a <see cref="VmaAllocated"/> backing.</summary>
    public static implicit operator ImageBacking(VmaAllocation vmaAllocation) => _VmaAllocated(vmaAllocation);
}
