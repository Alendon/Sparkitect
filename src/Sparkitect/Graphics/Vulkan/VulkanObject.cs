using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public abstract class VulkanObject : IDisposable
{
    public bool IsDisposed { get; private set; }
    private readonly IObjectTracker<VulkanObject>.Handle _trackerHandle;
    protected IVulkanContext VulkanContext { get; }
    protected Vk Vk => VulkanContext.VkApi;
    protected unsafe AllocationCallbacks* AllocationCallbacks => VulkanContext.DefaultAllocationCallbacks;
    protected Device Device => VulkanContext.VkDevice.Handle;

    protected VulkanObject(IVulkanContext vulkanContext, CallerContext callerContext = default)
    {
        VulkanContext = vulkanContext;
        _trackerHandle = VulkanContext.ObjectTracker.Track(this, callerContext);
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        Destroy();
        IsDisposed = true;
    }
    
    public void MarkDisposed()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        IsDisposed = true;
    }

    public abstract void Destroy();
}