using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkCommandBuffer : VulkanObject
{
    public VkCommandBuffer(CommandBuffer commandBuffer, IVulkanContext vulkanContext, VkCommandPool pool,
        CallerContext callerContext = default) : base(vulkanContext, callerContext)
    {
        Handle = commandBuffer;
        ParentPool = pool;
    }

    public CommandBuffer Handle { get; }
    public VkCommandPool ParentPool { get; }


    public override void Destroy()
    {
        ParentPool.FreeCommandBuffers(this);
    }
}