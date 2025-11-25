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

    protected VulkanObject(IVulkanContext vulkanContext)
    {
        VulkanContext = vulkanContext;
        _trackerHandle = VulkanContext.ObjectTracker.Track(this);
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        IsDisposed = true;
        Destroy();
    }
    
    public void MarkDisposed()
    {
        if (IsDisposed) return;

        _trackerHandle.Free();
        IsDisposed = true;
    }

    public abstract void Destroy();
}