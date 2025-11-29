using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkShaderModule : VulkanObject
{
    internal unsafe VkShaderModule(ShaderModule handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public ShaderModule Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyShaderModule(Device, Handle, AllocationCallbacks);
    }
}
