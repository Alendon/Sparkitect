namespace Sparkitect.Graphics.Vulkan.Vma;

public sealed class VmaDefragmentationContext : IDisposable
{
    private readonly VmaAllocator _allocator;
    private bool _disposed;

    internal nint Handle { get; }

    internal VmaDefragmentationContext(VmaAllocator allocator, nint handle)
    {
        _allocator = allocator;
        Handle = handle;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _allocator.EndDefragmentation(this);
    }
}
