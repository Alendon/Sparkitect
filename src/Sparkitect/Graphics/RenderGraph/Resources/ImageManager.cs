using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The concrete leaf backing provider for swapchain-origin images. Holds the applied swapchain and the
/// current frame's acquired index; its resolve reads that index's backing and constructs a single-index leaf.
/// </summary>
[GraphLocal<IImageManager, IRenderGraph>]
internal sealed class ImageManager : IImageManager
{
    private readonly IVulkanContext _vulkanContext;
    private VkSwapchain? _swapchain;
    private uint _acquiredIndex;

    // Allocated once and reused for the graph's lifetime; not the cross-pass identity source.
    private ImageResource? _transientLeaf;

    public ImageManager(IVulkanContext vulkanContext) => _vulkanContext = vulkanContext;

    /// <inheritdoc/>
    public void SetSwapchain(VkSwapchain swapchain) => _swapchain = swapchain;

    /// <inheritdoc/>
    public void InformAcquiredIndex(uint index) => _acquiredIndex = index;

    /// <inheritdoc/>
    public ImageResource ResolveSwapchainLeaf()
    {
        if (_swapchain is null)
            throw new InvalidOperationException(
                "SwapchainImageBackingProvider.ResolveSwapchainLeaf: swapchain not applied. " +
                "Call SetSwapchain(VkSwapchain) before resolving a swapchain leaf.");

        var backing = _swapchain.Images[(int)_acquiredIndex];
        return new ImageResource(
            backing,
            _swapchain.Extent,
            _swapchain.ImageFormat,
            initialLayout: ImageLayout.Undefined,
            initialAccess: 0);
    }

    /// <inheritdoc/>
    public ImageResource ResolveTransientLeaf(ExtentIntent intent, Format format)
    {
        if (_transientLeaf is not null)
            return _transientLeaf;

        if (_swapchain is null)
            throw new InvalidOperationException(
                "ImageManager.ResolveTransientLeaf: swapchain not applied. " +
                "Call SetSwapchain(VkSwapchain) before resolving a transient leaf.");

        var extent = intent switch
        {
            ExtentIntent.MatchSwapchain => _swapchain.Extent,
        };

        var result = _vulkanContext.CreateStorageImage2D(extent, format);
        if (result is not Result<VkImage, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                "ImageManager.ResolveTransientLeaf: CreateStorageImage2D failed " +
                $"({((Result<VkImage, VkApiResult>.Error)result).Value}).");

        _transientLeaf = new ImageResource(
            ok.Value,
            extent,
            format,
            initialLayout: ImageLayout.Undefined,
            initialAccess: 0);
        return _transientLeaf;
    }

    /// <inheritdoc/>
    public void DisposeTransient()
    {
        // Freeing a transient today fully disposes its VMA backing; resource aliasing will replace this
        // with reuse of a shared allocation.
        _transientLeaf?.Backing.Dispose();
        _transientLeaf = null;
    }
}
