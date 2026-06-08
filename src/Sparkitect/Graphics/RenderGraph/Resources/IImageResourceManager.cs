using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Per-graph image resource manager. Owns physical image backings (swapchain + transients)
/// and coordinates frame-aliasing. Does NOT author per-pass barrier emission — that lives
/// on <see cref="Hooks.IPreExecuteHook"/> implementations on resource views.
/// </summary>
[PublicAPI]
public interface IImageResourceManager :
    IGraphResourceManager<Image, ImageRequest>,
    IGraphResourceManager<WriteableImage, WriteableImageRequest>,
    IGraphResourceManager<ReadableImage, ReadableImageRequest>,
    IGraphResourceManager<StorageImageView, StorageImageViewRequest>
{
    /// <summary>
    /// Bind the graphics queue family the manager will use when constructing new
    /// swapchain-backed images. Invoke during render-graph setup, before any Apply.
    /// </summary>
    void BindQueueFamily(uint queueFamily);

    /// <summary>
    /// Drain the module resource-registration store, creating one shared backing image per
    /// registered <c>(Identification, ImageDescription)</c>. Invoke once during render-graph
    /// setup, after <see cref="BindQueueFamily"/>; <c>FromRegistered</c> declarations resolve
    /// against the backings created here.
    /// </summary>
    void DrainRegisteredImages();

    /// <summary>
    /// Apply each registered image's <c>ImageDescription.DefaultFill</c> exactly once, using
    /// the supplied recording command buffer. Invoked from the first frame after the frame
    /// command buffer begins recording (no Setup-time recording surface exists). A no-op on
    /// every call after the first.
    /// </summary>
    void ApplyPendingFills(VkCommandBuffer commandBuffer);

    /// <summary>
    /// Bind (or rebind) the swapchain backing, reading its images/extent/format directly.
    /// Reached through <see cref="ISwapchainHandler"/>; rebinds tracked handles.
    /// </summary>
    void Apply(VkSwapchain swapchain);
    void BeginFrame(uint acquiredSwapchainImageIndex);
    void EndFrame(VkCommandBuffer commandBuffer);
}
