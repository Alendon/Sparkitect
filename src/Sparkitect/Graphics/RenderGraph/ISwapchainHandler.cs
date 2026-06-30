using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Receives the engine's swapchain as externally-managed state. The swapchain is not a graph
/// resource — it is delivered after graph construction (and on every subsequent re-publish / resize)
/// through this handler, reached via <see cref="IRenderGraph.GetHandler{THandler}"/>.
/// </summary>
[PublicAPI]
public interface ISwapchainHandler
{
    /// <summary>Bind (or rebind) the swapchain the graph renders into.</summary>
    void SetSwapchain(VkSwapchain swapchain);
}
