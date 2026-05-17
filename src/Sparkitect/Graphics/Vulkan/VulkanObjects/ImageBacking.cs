using Sparkitect.Graphics.Vulkan.Vma;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Discriminates how a <see cref="VkImage"/>'s native handle is backed: either
/// owned by a swapchain (no per-image destruction) or allocated through VMA.
/// </summary>
[DiscriminatedUnion]
public abstract partial record ImageBacking
{
    public sealed partial record Swapchain : ImageBacking;
    public sealed partial record VmaAllocated(VmaAllocation Value) : ImageBacking;

    public static implicit operator ImageBacking(VmaAllocation vmaAllocation) => _VmaAllocated(vmaAllocation);
}
