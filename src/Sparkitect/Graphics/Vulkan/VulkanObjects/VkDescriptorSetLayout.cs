using JetBrains.Annotations;
using Silk.NET.Vulkan;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkDescriptorSetLayout : VulkanObject
{
    internal unsafe VkDescriptorSetLayout(DescriptorSetLayout handle, IVulkanContext vulkanContext)
        : base(vulkanContext)
    {
        Handle = handle;
    }

    public DescriptorSetLayout Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyDescriptorSetLayout(Device, Handle, AllocationCallbacks);
    }
}
