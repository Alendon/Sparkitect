using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a Vulkan fence used to synchronize the host with GPU work completion.</summary>
[PublicAPI]
public class VkFence : VulkanObject
{
    /// <summary>Wraps an existing <see cref="Fence"/> handle, tracked against <paramref name="vulkanContext"/>.</summary>
    public VkFence(Fence handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    /// <summary>The underlying Silk.NET <see cref="Fence"/> handle.</summary>
    public Fence Handle { get; }

    /// <summary>Blocks until the fence is signaled or <paramref name="timeout"/> nanoseconds elapse.</summary>
    public unsafe Result Wait(ulong timeout = ulong.MaxValue)
    {
        var fence = Handle;
        return Vk.WaitForFences(Device, 1, &fence, true, timeout);
    }

    /// <summary>Returns the fence to the unsignaled state.</summary>
    public unsafe Result Reset()
    {
        var fence = Handle;
        return Vk.ResetFences(Device, 1, &fence);
    }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        Vk.DestroyFence(Device, Handle, AllocationCallbacks);
    }
}
