using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkPipelineLayout : VulkanObject
{
    internal VkPipelineLayout(PipelineLayout handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public PipelineLayout Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyPipelineLayout(Device, Handle, AllocationCallbacks);
    }
}
