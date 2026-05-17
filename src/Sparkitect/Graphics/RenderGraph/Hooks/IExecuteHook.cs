using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Hooks;

/// <summary>
/// Lifecycle hook invoked once per pass per frame.
/// </summary>
public interface IExecuteHook
{
    void Execute(VkCommandBuffer commandBuffer, uint swapchainImageIndex);
}
