using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public abstract class VulkanObject : IDisposable
{
    public bool IsDisposed { get; private set; }
    private readonly IObjectTracker<VulkanObject> _objectTracker;
    protected Vk Vk { get; }

    protected VulkanObject(IObjectTracker<VulkanObject> objectTracker, Vk vk)
    {
        _objectTracker = objectTracker;
        Vk = vk;
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        _objectTracker.Untrack(this);
        IsDisposed = true;
        Destroy();
    }

    public abstract void Destroy();
}