using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Graphics.Vulkan.Vma;

public sealed class VmaBuffer : IDisposable
{
    private readonly VmaAllocator _allocator;
    private bool _disposed;

    public Buffer Buffer { get; }
    public VmaAllocation Allocation { get; }
    public VmaAllocationInfo AllocationInfo { get; }

    internal VmaBuffer(VmaAllocator allocator, Buffer buffer, VmaAllocation allocation, VmaAllocationInfo allocationInfo)
    {
        _allocator = allocator;
        Buffer = buffer;
        Allocation = allocation;
        AllocationInfo = allocationInfo;

        // Register with resource tracker for leak detection
        _allocator.ResourceTracker.Track(this);
    }

    public nint Map() => _allocator.MapMemory(Allocation);

    public void Unmap() => _allocator.UnmapMemory(Allocation);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unregister from resource tracker
        _allocator.ResourceTracker.Untrack(this);
        _allocator.DestroyBuffer(this);
    }
}
