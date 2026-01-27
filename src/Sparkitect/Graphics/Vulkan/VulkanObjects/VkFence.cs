using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkFence : VulkanObject
{
    internal unsafe VkFence(Fence handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public Fence Handle { get; }

    public unsafe Result Wait(ulong timeout = ulong.MaxValue)
    {
        var fence = Handle;
        return Vk.WaitForFences(Device, 1, &fence, true, timeout);
    }

    public unsafe Result Reset()
    {
        var fence = Handle;
        return Vk.ResetFences(Device, 1, &fence);
    }

    public override unsafe void Destroy()
    {
        Vk.DestroyFence(Device, Handle, AllocationCallbacks);
    }
}
