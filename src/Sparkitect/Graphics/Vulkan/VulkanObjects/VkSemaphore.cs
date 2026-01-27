using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkSemaphore : VulkanObject
{
    internal unsafe VkSemaphore(Silk.NET.Vulkan.Semaphore handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public Silk.NET.Vulkan.Semaphore Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroySemaphore(Device, Handle, AllocationCallbacks);
    }
}
