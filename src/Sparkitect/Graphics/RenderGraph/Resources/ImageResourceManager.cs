using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Single Image bound to the swapchain plus a per-graph pool of transient Images.
/// Acts as the dispatch coordinator for image resources — it owns physical resources
/// and frame-index rotation, but does NOT author barrier emission (views do, via
/// <see cref="Hooks.IPreExecuteHook"/>).
/// </summary>
internal sealed class ImageResourceManager : IImageResourceManager, IDisposable
{
    private readonly Image _swapchainImage;
    private readonly IVulkanContext? _vulkanContext;
    private readonly List<Image> _transients = new();

    internal ImageResourceManager(Image swapchainImage, IVulkanContext? vulkanContext = null)
    {
        _swapchainImage = swapchainImage;
        _vulkanContext = vulkanContext;
    }

    public IGraphResource<WriteableImage> Declare(
        Identification passId, int slot, WriteableImageRequest request)
    {
        return request switch
        {
            WriteableImageRequest.FromSwapchain swap
                => new Handle<WriteableImage>(slot, new WriteableImage(_swapchainImage, swap.Usage)),
            WriteableImageRequest.FromTransient transient
                => new Handle<WriteableImage>(slot, new WriteableImage(
                    RegisterTransient(AllocateTransient(transient.Extent, transient.Format)),
                    transient.Usage)),
        };
    }

    public IGraphResource<Image> Declare(
        Identification passId, int slot, ImageRequest request)
    {
        return request switch
        {
            ImageRequest.FromSwapchain
                => new Handle<Image>(slot, _swapchainImage),
            ImageRequest.FromTransient transient
                => new Handle<Image>(slot, RegisterTransient(AllocateTransient(transient.Extent, transient.Format))),
        };
    }

    public void BeginFrame(uint acquiredSwapchainImageIndex)
        => _swapchainImage.SetCurrentIndex((int)acquiredSwapchainImageIndex);

    public void EndFrame(VkCommandBuffer commandBuffer)
        => _swapchainImage.TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            newAccess: 0,
            dstStage: PipelineStageFlags.BottomOfPipeBit);

    public void Dispose()
    {
        foreach (var t in _transients)
        {
            foreach (var i in EnumerateBackings(t)) i.Dispose();
        }
        _transients.Clear();
    }

    private static IEnumerable<VkImage> EnumerateBackings(Image image)
    {
        for (var i = 0; i < image.BackingCount; i++)
        {
            image.SetCurrentIndex(i);
            yield return image.CurrentVkImage;
        }
    }

    private Image RegisterTransient(Image transient)
    {
        _transients.Add(transient);
        return transient;
    }

    private Image AllocateTransient(Extent2D extent, Format format)
    {
        if (_vulkanContext is null)
            throw new InvalidOperationException(
                "ImageResourceManager: transient image declared but no IVulkanContext supplied at construction.");

        var result = _vulkanContext.CreateStorageImage2D(extent, format);
        if (result is not Result<VkImage, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"ImageResourceManager: CreateStorageImage2D failed ({((Result<VkImage, VkApiResult>.Error)result).Value}).");

        return new Image(new[] { ok.Value }, extent, format, initialQueueFamily: 0);
    }

    private sealed class Handle<TView> : IGraphResource<TView>
    {
        private readonly TView _view;
        public Handle(int slot, TView view) { Slot = slot; _view = view; }
        public int Slot { get; }
        public TView Fetch() => _view;
    }
}
