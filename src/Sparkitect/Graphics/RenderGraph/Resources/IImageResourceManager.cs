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
    void BeginFrame(uint acquiredSwapchainImageIndex);
    void EndFrame(VkCommandBuffer commandBuffer);
}
