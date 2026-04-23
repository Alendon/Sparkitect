using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Tracked VMA-backed Vulkan image wrapper. Owns a <see cref="VmaAllocation"/> + descriptor
/// and routes <see cref="VmaResource.Destroy"/> through the creating
/// <see cref="ManagedVmaAllocator"/>.
/// </summary>
public sealed class VmaImage : VmaResource
{
    /// <summary>The underlying Silk.NET Vulkan image handle.</summary>
    public Image Image { get; }

    /// <summary>Opaque VMA allocation paired with the <see cref="Image"/> handle.</summary>
    public VmaAllocation Allocation { get; }

    /// <summary>Allocation descriptor (memory type, offset, size).</summary>
    public VmaAllocationInfo AllocationInfo { get; }

    internal VmaImage(
        ManagedVmaAllocator allocator,
        Image image,
        VmaAllocation allocation,
        VmaAllocationInfo allocationInfo,
        CallerContext callerContext)
        : base(allocator, callerContext)
    {
        Image = image;
        Allocation = allocation;
        AllocationInfo = allocationInfo;
    }

    /// <inheritdoc />
    public override void Destroy() => Allocator.DestroyImage(Image, Allocation);
}
