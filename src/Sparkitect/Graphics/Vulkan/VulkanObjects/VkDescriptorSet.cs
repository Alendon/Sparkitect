using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkDescriptorSet : VulkanObject
{
    internal VkDescriptorSet(DescriptorSet handle, IVulkanContext vulkanContext, VkDescriptorPool parentPool, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
        ParentPool = parentPool;
    }

    public DescriptorSet Handle { get; }
    public VkDescriptorPool ParentPool { get; }

    public override void Destroy()
    {
        // DescriptorSets are implicitly freed when their pool is destroyed.
        // Do not call vkFreeDescriptorSets here - pool handles cleanup.
    }
}
