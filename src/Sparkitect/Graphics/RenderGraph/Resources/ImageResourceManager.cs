using JetBrains.Annotations;
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
/// construction via <see cref="Apply"/> (reached through <see cref="ISwapchainHandler"/>);
/// Handles handed out before <see cref="Apply"/> run are tracked and rebound when
/// <see cref="Apply"/> is called (and re-bound again on every subsequent call,
/// supporting future resize / re-publish flows).
/// </summary>
[GraphLocal<IImageResourceManager>]
[PublicAPI]
public sealed class ImageResourceManager : IImageResourceManager, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly IResourceRegistrationStore _registrationStore;
    private readonly List<Image> _transients = new();
    private readonly List<ISwapchainTrackedHandle> _swapchainHandles = new();
    private readonly Dictionary<Identification, Image> _registeredBackings = new();
    private readonly List<(Image Image, ClearColorValue Fill)> _pendingFills = new();
    private readonly List<StorageImageView> _storageViews = new();
    private Image? _swapchainImage;
    private uint _queueFamily;
    private bool _fillsApplied;

    internal ImageResourceManager(IVulkanContext vulkanContext, IResourceRegistrationStore registrationStore)
    {
        _vulkanContext = vulkanContext;
        _registrationStore = registrationStore;
    }

    /// <summary>
    /// Binds the graphics queue family the manager will use when constructing new
    /// swapchain-backed images. Invoke during render-graph setup, before any Apply.
    /// </summary>
    public void BindQueueFamily(uint queueFamily)
    {
        _queueFamily = queueFamily;
    }

    public void DrainRegisteredImages()
    {
        foreach (var (id, description) in _registrationStore.RegisteredImages)
        {
            if (_registeredBackings.ContainsKey(id)) continue;

            var backing = AllocateBacking(description.Size, description.Format);
            _registeredBackings[id] = backing;

            if (description.DefaultFill is { } fill)
                _pendingFills.Add((backing, fill));
        }
    }

    public void ApplyPendingFills(VkCommandBuffer commandBuffer)
    {
        if (_fillsApplied) return;
        _fillsApplied = true;

        foreach (var (image, fill) in _pendingFills)
        {
            image.TransitionTo(
                commandBuffer,
                ImageLayout.TransferDstOptimal,
                AccessFlags.TransferWriteBit,
                PipelineStageFlags.TransferBit);
            commandBuffer.ClearColorImage(image.CurrentVkImage, ImageLayout.TransferDstOptimal, in fill);
        }
        _pendingFills.Clear();
    }

    public void Apply(VkSwapchain swapchain)
    {
        var backings = swapchain.Images.ToArray();
        var newImage = new Image(
            backings,
            swapchain.Extent,
            swapchain.ImageFormat,
            initialQueueFamily: _queueFamily);

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
            WriteableImageRequest.FromRegistered registered
                => new TransientWriteableImageHandle(
                    slot,
                    new WriteableImage(ResolveRegistered(registered.Id, passId), registered.Usage)),
        };
    }

    public IGraphResource<ReadableImage> Declare(
        Identification passId, int slot, ReadableImageRequest request)
    {
        return request switch
        {
            ReadableImageRequest.FromSwapchain swap
                => Track(new SwapchainReadableImageHandle(slot, swap.Usage, _swapchainImage)),
            ReadableImageRequest.FromTransient transient
                => new TransientReadableImageHandle(
                    slot,
                    new ReadableImage(
                        RegisterTransient(AllocateTransient(transient.Description.Size, transient.Description.Format)),
                        transient.Usage)),
            ReadableImageRequest.FromRegistered registered
                => new TransientReadableImageHandle(
                    slot,
                    new ReadableImage(ResolveRegistered(registered.Id, passId), registered.Usage)),
        };
    }

    public IGraphResource<StorageImageView> Declare(
        Identification passId, int slot, StorageImageViewRequest request)
    {
        var image = request switch
        {
            StorageImageViewRequest.FromRegistered registered
                => ResolveRegistered(registered.Id, passId),
            StorageImageViewRequest.FromTransient transient
                => RegisterTransient(AllocateTransient(transient.Description.Size, transient.Description.Format)),
        };

        var view = new StorageImageView(image, CreateViewsPerBacking(image, passId, slot));
        _storageViews.Add(view);
        return new TransientStorageImageViewHandle(slot, view);
    }

    private VkImageView[] CreateViewsPerBacking(Image image, Identification passId, int slot)
    {
        var views = new VkImageView[image.BackingCount];
        var savedIndex = image.CurrentBackingIndex;
        try
        {
            for (var i = 0; i < image.BackingCount; i++)
            {
                image.SetCurrentIndex(i);
                var result = image.CurrentVkImage.CreateView(ImageAspectFlags.ColorBit);
                if (result is not Result<VkImageView, VkApiResult>.Ok ok)
                    throw new InvalidOperationException(
                        $"ImageResourceManager.Declare: pass {passId} slot {slot} StorageImageView backing {i} " +
                        $"CreateView failed ({((Result<VkImageView, VkApiResult>.Error)result).Value}).");
                views[i] = ok.Value;
            }
        }
        finally
        {
            image.SetCurrentIndex(savedIndex);
        }

        return views;
    }

    private Image ResolveRegistered(Identification id, Identification passId)
    {
        if (_registeredBackings.TryGetValue(id, out var backing))
            return backing;

        throw new InvalidOperationException(
            $"ImageResourceManager.Declare: pass {passId} declared FromRegistered({id}) but no shared backing " +
            $"was drained for that identification. Register the image via GraphImageRegistry before graph setup.");
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
                "ImageResourceManager.BeginFrame: Swapchain not applied. Call Apply(VkSwapchain) before running frames.");
        _swapchainImage.SetCurrentIndex((int)acquiredSwapchainImageIndex);
    }

    public void EndFrame(VkCommandBuffer commandBuffer)
    {
        if (_swapchainImage is null)
            throw new InvalidOperationException(
                "ImageResourceManager.EndFrame: Swapchain not applied. Call Apply(VkSwapchain) before running frames.");
        _swapchainImage.TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            newAccess: 0,
            dstStage: PipelineStageFlags.BottomOfPipeBit);
    }

    public void Dispose()
    {
        foreach (var view in _storageViews) view.Dispose();
        _storageViews.Clear();
        foreach (var t in _transients)
        {
            foreach (var i in EnumerateBackings(t)) i.Dispose();
        }
        foreach (var b in _registeredBackings.Values)
        {
            foreach (var i in EnumerateBackings(b)) i.Dispose();
        }
        _transients.Clear();
        _registeredBackings.Clear();
        _pendingFills.Clear();
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
        var result = _vulkanContext.CreateStorageImage2D(extent, format);
        if (result is not Result<VkImage, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"ImageResourceManager: CreateStorageImage2D failed ({((Result<VkImage, VkApiResult>.Error)result).Value}).");

        return new Image(new[] { ok.Value }, extent, format, initialQueueFamily: 0);
    }

    private Image AllocateBacking(Extent2D extent, Format format)
    {
        var result = _vulkanContext.CreateStorageImage2D(extent, format);
        if (result is not Result<VkImage, VkApiResult>.Ok ok)
            throw new InvalidOperationException(
                $"ImageResourceManager: CreateStorageImage2D failed ({((Result<VkImage, VkApiResult>.Error)result).Value}).");

        return new Image(new[] { ok.Value }, extent, format, initialQueueFamily: _queueFamily);
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
                "IGraphResource<Image>.Fetch: Swapchain not applied. The owning ImageResourceManager has not yet received Apply(VkSwapchain).");
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
                "IGraphResource<WriteableImage>.Fetch: Swapchain not applied. The owning ImageResourceManager has not yet received Apply(VkSwapchain).");
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

    private sealed class SwapchainReadableImageHandle : IGraphResource<ReadableImage>, ISwapchainTrackedHandle
    {
        private readonly ReadUsage _usage;
        private ReadableImage? _view;
        public SwapchainReadableImageHandle(int slot, ReadUsage usage, Image? initialImage)
        {
            Slot = slot;
            _usage = usage;
            _view = initialImage is null ? null : new ReadableImage(initialImage, usage);
        }
        public int Slot { get; }
        public ReadableImage Fetch() =>
            _view ?? throw new InvalidOperationException(
                "IGraphResource<ReadableImage>.Fetch: Swapchain not applied. The owning ImageResourceManager has not yet received Apply(VkSwapchain).");
        public void UpdateSwapchainImage(Image newSwapchainImage) =>
            _view = new ReadableImage(newSwapchainImage, _usage);
    }

    private sealed class TransientReadableImageHandle : IGraphResource<ReadableImage>
    {
        private readonly ReadableImage _view;
        public TransientReadableImageHandle(int slot, ReadableImage view) { Slot = slot; _view = view; }
        public int Slot { get; }
        public ReadableImage Fetch() => _view;
    }

    private sealed class TransientStorageImageViewHandle : IGraphResource<StorageImageView>
    {
        private readonly StorageImageView _view;
        public TransientStorageImageViewHandle(int slot, StorageImageView view) { Slot = slot; _view = view; }
        public int Slot { get; }
        public StorageImageView Fetch() => _view;
    }
}
