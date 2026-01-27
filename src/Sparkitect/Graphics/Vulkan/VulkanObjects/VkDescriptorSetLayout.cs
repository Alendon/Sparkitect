using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkDescriptorSetLayout : VulkanObject
{
    internal VkDescriptorSetLayout(DescriptorSetLayout handle, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
    }

    public DescriptorSetLayout Handle { get; }

    public override unsafe void Destroy()
    {
        Vk.DestroyDescriptorSetLayout(Device, Handle, AllocationCallbacks);
    }
}
