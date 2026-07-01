using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The concrete leaf backing provider for swapchain-origin images. It holds the applied swapchain and
/// the index the graph informed for the current frame; its resolve reads that index's backing and
/// constructs a single-index leaf. It owns no per-index arrays — the index lives here, in the resolve.
/// </summary>
[PublicAPI]
[GraphLocal<IImageManager, IRenderGraph>]
public sealed class ImageManager : IImageManager
{
    private readonly IVulkanContext _vulkanContext;
    private VkSwapchain? _swapchain;
    private uint _acquiredIndex;

    // The per-graph VMA allocation cache for the transient storage image: allocated once and reused for the
    // graph's lifetime so it is not reallocated every frame. This is NOT the cross-pass identity source —
    // shared-target identity flows through the graph's chain-keyed resolution (the sub-declared leaf marked
    // with the target moment), not through this singleton.
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
        // Allocation cache: the transient backing is allocated once and reused for the graph's lifetime
        // (removing this cache would reallocate the storage image every frame). Cross-pass identity is the
        // graph's concern — resolved through the chain-keyed instance context, not read back from here.
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
}
