using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Execute-time payload handed to compute-category passes. Carries the command buffer
/// the pass body may record into.
/// </summary>
public readonly struct ComputePassExecutePayload
{
    public VkCommandBuffer CommandBuffer { get; }

    public ComputePassExecutePayload(VkCommandBuffer commandBuffer)
    {
        CommandBuffer = commandBuffer;
    }
}
