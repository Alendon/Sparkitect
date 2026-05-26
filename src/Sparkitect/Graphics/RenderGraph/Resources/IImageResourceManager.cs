using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Per-graph image resource manager. Owns physical image backings (swapchain + transients)
/// and coordinates frame-aliasing. Does NOT author per-pass barrier emission — that lives
/// on <see cref="Hooks.IPreExecuteHook"/> implementations on resource views.
/// </summary>
internal interface IImageResourceManager :
    IGraphResourceManager<Image, ImageRequest>,
    IGraphResourceManager<WriteableImage, WriteableImageRequest>
{
    /// <summary>
    /// Bind the graphics queue family the manager will use when constructing new
    /// swapchain-backed images. Invoke during render-graph setup, before any Apply.
    /// </summary>
    void BindQueueFamily(uint queueFamily);

    void Apply(SwapchainResource swapchainResource);
    void BeginFrame(uint acquiredSwapchainImageIndex);
    void EndFrame(VkCommandBuffer commandBuffer);
}
