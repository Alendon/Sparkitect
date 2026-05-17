using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkSampler : VulkanObject
{
    public VkSampler(Sampler handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public Sampler Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroySampler(Device, Handle, AllocationCallbacks);
    }
}
