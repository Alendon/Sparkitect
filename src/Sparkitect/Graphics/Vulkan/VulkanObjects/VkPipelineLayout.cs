using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPipelineLayout : VulkanObject
{
    internal unsafe VkPipelineLayout(PipelineLayout handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public PipelineLayout Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyPipelineLayout(Device, Handle, AllocationCallbacks);
    }
}
