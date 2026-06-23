using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Hooks;

/// <summary>
/// Resource-view lifecycle hook invoked before a pass executes. Implementing types
/// perform synchronization, layout transitions, descriptor binding, or other
/// pre-execution work; they may go through resource helpers or issue raw Vulkan
/// commands directly as long as they keep the resource's tracked state coherent.
/// </summary>
[PublicAPI]
public interface IPreExecuteHook
{
    void PreExecute(VkCommandBuffer commandBuffer);
}
