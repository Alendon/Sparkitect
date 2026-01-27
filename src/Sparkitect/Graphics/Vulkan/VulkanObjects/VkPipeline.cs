using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPipeline : VulkanObject
{
    internal unsafe VkPipeline(Pipeline handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public Pipeline Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyPipeline(Device, Handle, AllocationCallbacks);
    }
}
