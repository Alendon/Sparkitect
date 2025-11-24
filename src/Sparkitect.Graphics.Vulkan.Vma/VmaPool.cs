namespace Sparkitect.Graphics.Vulkan.Vma;

public sealed class VmaPool : IDisposable
{
    private readonly VmaAllocator _allocator;
    private bool _disposed;

    internal nint Handle { get; }

    internal VmaPool(VmaAllocator allocator, nint handle)
    {
        _allocator = allocator;
        Handle = handle;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _allocator.DestroyPool(this);
    }
}
