using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Abstract base for VMA-backed tracked resources (buffers, images, future pool-backed allocations).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VmaResource"/> is parallel in shape to
/// <see cref="Sparkitect.Graphics.Vulkan.VulkanObject"/> but is a separate inheritance chain:
/// VMA resources are NOT tracked by <c>VulkanContext.ObjectTracker</c>. Each
/// <see cref="ManagedVmaAllocator"/> owns its own <see cref="IObjectTracker{T}"/> instance,
/// and every resource created through that allocator is registered on that per-allocator tracker.
/// </para>
/// <para>
/// The base class handles tracker registration (in the constructor) and tracker untracking
/// (in <see cref="Dispose"/>). Subclasses implement <see cref="Destroy"/> to free the native
/// VMA allocation, typically by routing the call through the owning
/// <see cref="Allocator"/>'s internal destroy methods.
/// </para>
/// </remarks>
public abstract class VmaResource : IDisposable
{
    private readonly IObjectTracker<VmaResource>.Handle _trackerHandle;

    /// <summary>
    /// The managed allocator that owns this resource. Subclasses route <see cref="Destroy"/>
    /// through this allocator (e.g. <see cref="ManagedVmaAllocator.DestroyBuffer"/>).
    /// </summary>
    public ManagedVmaAllocator Allocator { get; }

    /// <summary>
    /// Whether <see cref="Dispose"/> has been called on this resource.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Registers this resource against the allocator's per-allocator resource tracker.
    /// Subclasses call this from their constructor.
    /// </summary>
    /// <param name="allocator">The owning managed allocator.</param>
    /// <param name="callerContext">Caller-context capture for leak attribution.</param>
    protected VmaResource(ManagedVmaAllocator allocator, CallerContext callerContext = default)
    {
        Allocator = allocator;
        _trackerHandle = allocator.ObjectTracker.Track(this, callerContext);
    }

    /// <summary>
    /// Frees the tracker handle and invokes <see cref="Destroy"/>. Idempotent: a second call is a no-op.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        Destroy();
        IsDisposed = true;
    }

    /// <summary>
    /// Frees the native VMA allocation. Implementations route through the owning
    /// <see cref="Allocator"/>'s internal destroy methods (e.g.
    /// <c>Allocator.DestroyBuffer(handle, allocation)</c>).
    /// </summary>
    public abstract void Destroy();
}
