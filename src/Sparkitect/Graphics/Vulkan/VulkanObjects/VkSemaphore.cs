using JetBrains.Annotations;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkSemaphore : VulkanObject
{
    public VkSemaphore(Silk.NET.Vulkan.Semaphore handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
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
