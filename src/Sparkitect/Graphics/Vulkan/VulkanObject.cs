using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

/// <summary>
/// Base for owned Vulkan handles. Registers with the context's object tracker on construction
/// and destroys the native handle on <see cref="Dispose"/>.
/// </summary>
[PublicAPI]
public abstract class VulkanObject : IDisposable
{
    /// <summary>Whether the native handle has already been destroyed or released.</summary>
    public bool IsDisposed { get; private set; }
    private readonly IObjectTracker<VulkanObject>.Handle _trackerHandle;

    /// <summary>The Vulkan context that created and tracks this object.</summary>
    protected IVulkanContext VulkanContext { get; }

    /// <summary>The raw Silk.NET API entry point.</summary>
    protected Vk Vk => VulkanContext.VkApi;

    /// <summary>The allocation callbacks passed to every native create/destroy call.</summary>
    protected unsafe AllocationCallbacks* AllocationCallbacks => VulkanContext.DefaultAllocationCallbacks;

    /// <summary>The logical device handle this object belongs to.</summary>
    protected Device Device => VulkanContext.VkDevice.Handle;

    /// <summary>Registers the object with the context's tracker, capturing <paramref name="callerContext"/> for leak diagnostics.</summary>
    protected VulkanObject(IVulkanContext vulkanContext, CallerContext callerContext = default)
    {
        VulkanContext = vulkanContext;
        _trackerHandle = VulkanContext.ObjectTracker.Track(this, callerContext);
    }

    /// <summary>Destroys the native handle and releases the tracker entry. Idempotent.</summary>
    public void Dispose()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        Destroy();
        IsDisposed = true;
    }

    internal void MarkDisposed()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        IsDisposed = true;
    }

    /// <summary>Destroys the underlying native handle. Called once by <see cref="Dispose"/>.</summary>
    public abstract void Destroy();
}
