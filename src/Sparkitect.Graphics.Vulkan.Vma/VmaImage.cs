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
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _allocator.DestroyImage(this);
    }
}
