using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;

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
    private VkSwapchain? _swapchain;
    private uint _acquiredIndex;

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
}
