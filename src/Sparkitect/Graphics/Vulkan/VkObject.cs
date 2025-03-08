global using AllocationHandler = object;

using JetBrains.Annotations;
using Silk.NET.Vulkan;


namespace Sparkitect.Graphics.Vulkan;

[PublicAPI]
public abstract class VkObject : IDisposable
{
    public bool IsDisposed { get; private set; }
    private AllocationHandler AllocationHandler { get; }
    protected Vk Vk { get; }

    protected VkObject(AllocationHandler allocationHandler, Vk vk)
    {
        AllocationHandler = allocationHandler;
        Vk = vk;
        // TODO: Add to allocation tracking
    }

    public void Dispose()
    {
        if (IsDisposed) return;

        //TODO: Remove from allocation tracking
        IsDisposed = true;
        Destroy();
    }

    public abstract void Destroy();
}