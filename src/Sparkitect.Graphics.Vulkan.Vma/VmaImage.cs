using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.Vma;

public sealed class VmaImage : IDisposable
{
    private readonly VmaAllocator _allocator;
    private bool _disposed;

    public Image Image { get; }
    public VmaAllocation Allocation { get; }
    public VmaAllocationInfo AllocationInfo { get; }

    internal VmaImage(VmaAllocator allocator, Image image, VmaAllocation allocation, VmaAllocationInfo allocationInfo)
    {
        _allocator = allocator;
        Image = image;
        Allocation = allocation;
        AllocationInfo = allocationInfo;

        // Register with resource tracker for leak detection
        _allocator.ResourceTracker.Track(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Unregister from resource tracker
        _allocator.ResourceTracker.Untrack(this);
        _allocator.DestroyImage(this);
    }
}
