using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPipeline : VulkanObject
{
    internal VkPipeline(Pipeline handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public Pipeline Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyPipeline(Device, Handle, AllocationCallbacks);
    }
}
