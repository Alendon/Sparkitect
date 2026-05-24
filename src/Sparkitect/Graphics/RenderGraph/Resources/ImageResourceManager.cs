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
/// <see cref="Hooks.IPreExecuteHook"/>). The swapchain backing is delivered after
/// construction via <see cref="Apply"/>; Handles handed out before <see cref="Apply"/>
/// run are tracked and rebound when <see cref="Apply"/> is called (and re-bound again
/// on every subsequent call, supporting future resize / re-publish flows).
/// </summary>
internal sealed class ImageResourceManager : IImageResourceManager, IDisposable
{
    private readonly IVulkanContext? _vulkanContext;
    private readonly List<Image> _transients = new();
    private readonly List<ISwapchainTrackedHandle> _swapchainHandles = new();
    private Image? _swapchainImage;
    private readonly uint _initialQueueFamily;

    internal ImageResourceManager(IVulkanContext? vulkanContext = null, uint initialQueueFamily = 0)
    {
        _vulkanContext = vulkanContext;
        _initialQueueFamily = initialQueueFamily;
    }

    public void Apply(SwapchainResource swapchainResource)
    {
        var backings = swapchainResource.Underlying.Images.ToArray();
        var newImage = new Image(
            backings,
            swapchainResource.Extent,
            swapchainResource.Format,
            initialQueueFamily: _initialQueueFamily);

        _swapchainImage = newImage;

        foreach (var handle in _swapchainHandles)
            handle.UpdateSwapchainImage(newImage);
    }

    public IGraphResource<WriteableImage> Declare(
        Identification passId, int slot, WriteableImageRequest request)
    {
        return request switch
        {
            WriteableImageRequest.FromSwapchain swap
                => Track(new SwapchainWriteableImageHandle(slot, swap.Usage, _swapchainImage)),
            WriteableImageRequest.FromTransient transient
                => new TransientWriteableImageHandle(
                    slot,
                    new WriteableImage(
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
                => Track(new SwapchainImageHandle(slot, _swapchainImage)),
            ImageRequest.FromTransient transient
                => new TransientImageHandle(
                    slot,
                    RegisterTransient(AllocateTransient(transient.Extent, transient.Format))),
        };
    }

    public void BeginFrame(uint acquiredSwapchainImageIndex)
    {
        if (_swapchainImage is null)
            throw new InvalidOperationException(
                "ImageResourceManager.BeginFrame: Swapchain not applied. Call Apply(SwapchainResource) before running frames.");
        _swapchainImage.SetCurrentIndex((int)acquiredSwapchainImageIndex);
    }

    public void EndFrame(VkCommandBuffer commandBuffer)
    {
        if (_swapchainImage is null)
            throw new InvalidOperationException(
                "ImageResourceManager.EndFrame: Swapchain not applied. Call Apply(SwapchainResource) before running frames.");
        _swapchainImage.TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            newAccess: 0,
            dstStage: PipelineStageFlags.BottomOfPipeBit);
    }

    public void Dispose()
    {
        foreach (var t in _transients)
        {
            foreach (var i in EnumerateBackings(t)) i.Dispose();
        }
        _transients.Clear();
        _swapchainHandles.Clear();
    }

    private THandle Track<THandle>(THandle handle) where THandle : ISwapchainTrackedHandle
    {
        _swapchainHandles.Add(handle);
        return handle;
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

    private interface ISwapchainTrackedHandle
    {
        void UpdateSwapchainImage(Image newSwapchainImage);
    }

    private sealed class SwapchainImageHandle : IGraphResource<Image>, ISwapchainTrackedHandle
    {
        private Image? _image;
        public SwapchainImageHandle(int slot, Image? initialImage)
        {
            Slot = slot;
            _image = initialImage;
        }
        public int Slot { get; }
        public Image Fetch() =>
            _image ?? throw new InvalidOperationException(
                "IGraphResource<Image>.Fetch: Swapchain not applied. The owning ImageResourceManager has not yet received Apply(SwapchainResource).");
        public void UpdateSwapchainImage(Image newSwapchainImage) => _image = newSwapchainImage;
    }

    private sealed class SwapchainWriteableImageHandle : IGraphResource<WriteableImage>, ISwapchainTrackedHandle
    {
        private readonly WriteUsage _usage;
        private WriteableImage? _view;
        public SwapchainWriteableImageHandle(int slot, WriteUsage usage, Image? initialImage)
        {
            Slot = slot;
            _usage = usage;
            _view = initialImage is null ? null : new WriteableImage(initialImage, usage);
        }
        public int Slot { get; }
        public WriteableImage Fetch() =>
            _view ?? throw new InvalidOperationException(
                "IGraphResource<WriteableImage>.Fetch: Swapchain not applied. The owning ImageResourceManager has not yet received Apply(SwapchainResource).");
        public void UpdateSwapchainImage(Image newSwapchainImage) =>
            _view = new WriteableImage(newSwapchainImage, _usage);
    }

    private sealed class TransientImageHandle : IGraphResource<Image>
    {
        private readonly Image _image;
        public TransientImageHandle(int slot, Image image) { Slot = slot; _image = image; }
        public int Slot { get; }
        public Image Fetch() => _image;
    }

    private sealed class TransientWriteableImageHandle : IGraphResource<WriteableImage>
    {
        private readonly WriteableImage _view;
        public TransientWriteableImageHandle(int slot, WriteableImage view) { Slot = slot; _view = view; }
        public int Slot { get; }
        public WriteableImage Fetch() => _view;
    }
}
