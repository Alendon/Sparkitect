using Silk.NET.Vulkan;
using Sparkitect.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Tracked VMA-backed Vulkan buffer wrapper. Owns a <see cref="VmaAllocation"/> + descriptor
/// and routes <see cref="Map"/>/<see cref="Unmap"/>/<see cref="VmaResource.Destroy"/> through
/// the creating <see cref="ManagedVmaAllocator"/>.
/// </summary>
public sealed class VmaBuffer : VmaResource
{
    /// <summary>The underlying Silk.NET Vulkan buffer handle.</summary>
    public Buffer Buffer { get; }

    /// <summary>Opaque VMA allocation paired with the <see cref="Buffer"/> handle.</summary>
    public VmaAllocation Allocation { get; }

    /// <summary>Allocation descriptor (memory type, offset, size, optionally mapped pointer).</summary>
    public VmaAllocationInfo AllocationInfo { get; }

    internal VmaBuffer(
        ManagedVmaAllocator allocator,
        Buffer buffer,
        VmaAllocation allocation,
        VmaAllocationInfo allocationInfo,
        CallerContext callerContext)
        : base(allocator, callerContext)
    {
        Buffer = buffer;
        Allocation = allocation;
        AllocationInfo = allocationInfo;
    }

    /// <summary>Maps the buffer's memory and returns a native pointer to the mapped range.</summary>
    public nint Map() => Allocator.Map(Allocation);

    /// <summary>Unmaps the buffer's memory, matching a prior <see cref="Map"/> call.</summary>
    public void Unmap() => Allocator.Unmap(Allocation);

    /// <inheritdoc />
    public override void Destroy() => Allocator.DestroyBuffer(Buffer, Allocation);
}
