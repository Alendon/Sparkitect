using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkShaderModule : VulkanObject
{
    public VkShaderModule(ShaderModule handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public ShaderModule Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyShaderModule(Device, Handle, AllocationCallbacks);
    }
}
