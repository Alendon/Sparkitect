using JetBrains.Annotations;
using Serilog;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns the swapchain, its per-image wrappers, and their views; recreated on resize or when out of date.</summary>
[PublicAPI]
public class VkSwapchain : VulkanObject
{
    private readonly KhrSwapchain _khrSwapchain;
    private SwapchainKHR _handle;
    private VkImage[] _images;
    private VkImageView[] _imageViews;
    private SwapchainConfig _config;

    /// <summary>The underlying Silk.NET <see cref="SwapchainKHR"/> handle.</summary>
    public SwapchainKHR Handle => _handle;

    /// <summary>The surface this swapchain presents to.</summary>
    public VkSurface Surface { get; }

    /// <summary>The format of the swapchain images.</summary>
    public Format ImageFormat { get; private set; }

    /// <summary>The color space of the swapchain images.</summary>
    public ColorSpaceKHR ColorSpace { get; private set; }

    /// <summary>The current image extent in pixels.</summary>
    public Extent2D Extent { get; private set; }

    /// <summary>The number of images in the swapchain.</summary>
    public uint ImageCount => (uint)_images.Length;

    /// <summary>The swapchain image wrappers, owned by the swapchain.</summary>
    public ReadOnlySpan<VkImage> Images => _images;

    /// <summary>The image views over <see cref="Images"/>.</summary>
    public ReadOnlySpan<VkImageView> ImageViews => _imageViews;

    /// <summary>Creates a swapchain for <paramref name="surface"/> sized to <paramref name="width"/> x <paramref name="height"/>.</summary>
    public VkSwapchain(
        VkSurface surface,
        SwapchainConfig config,
        IVulkanContext context, uint width, uint height) : base(context)
    {
        Surface = surface;
        _config = config;
        _images = [];
        _imageViews = [];

        if (!Vk.TryGetDeviceExtension(context.VkInstance.Handle, context.VkDevice.Handle, out _khrSwapchain))
            throw new InvalidOperationException("KHR_swapchain extension not available");

        CreateSwapchain(width, height);
    }

    /// <summary>
    /// Acquires the next available image from the swapchain.
    /// </summary>
    /// <param name="signalSemaphore">Semaphore to signal when image is available.</param>
    /// <param name="timeout">Timeout in nanoseconds. Default is infinite.</param>
    /// <param name="autoRecreate">If true, automatically recreates swapchain on OUT_OF_DATE.</param>
    /// <returns>The image index, or error result.</returns>
    public Result<uint, VkApiResult> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false)
    {
        uint imageIndex = uint.MaxValue;
        var semaphoreHandle = signalSemaphore.Handle;
        var result = _khrSwapchain.AcquireNextImage(
            Device, _handle, timeout, semaphoreHandle, (Fence)default, ref imageIndex);

        if (result == VkApiResult.ErrorOutOfDateKhr)
        {
            if (autoRecreate)
            {
                Recreate(Extent.Width, Extent.Height);
                result = _khrSwapchain.AcquireNextImage(
                    Device, _handle, timeout, semaphoreHandle, default, ref imageIndex);
            }
            else
            {
                return result;
            }
        }

        if (result != VkApiResult.Success && result != VkApiResult.SuboptimalKhr)
            return result;

        return imageIndex;
    }

    /// <summary>
    /// Presents an image to the given queue.
    /// </summary>
    /// <param name="imageIndex">Image index to present.</param>
    /// <param name="waitSemaphore">Semaphore to wait on before presenting.</param>
    /// <param name="presentQueue">Queue to present on.</param>
    /// <returns>Result code. Check for OUT_OF_DATE/SUBOPTIMAL.</returns>
    public unsafe VkApiResult Present(uint imageIndex, VkSemaphore waitSemaphore, VkQueue presentQueue)
    {
        var swapchain = _handle;
        var semaphoreHandle = waitSemaphore.Handle;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &semaphoreHandle,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex,
        };
        return _khrSwapchain.QueuePresent(presentQueue.Handle, in presentInfo);
    }

    /// <summary>
    /// Recreates the swapchain with new dimensions.
    /// Waits for device idle before destroying old resources.
    /// </summary>
    public void Recreate(uint width, uint height)
    {
        Vk.DeviceWaitIdle(Device);
        CreateSwapchain(width, height);
    }

    private unsafe void CreateSwapchain(uint requestedWidth, uint requestedHeight)
    {
        var physicalDevice = VulkanContext.VkPhysicalDevice.PhysicalDevice;
        var capabilities = Surface.GetCapabilities(physicalDevice);
        var formats = Surface.GetFormats(physicalDevice);
        var presentModes = Surface.GetPresentModes(physicalDevice);

        var surfaceFormat = SelectSurfaceFormat(formats);
        var presentMode = SelectPresentMode(presentModes);
        var extent = SelectExtent(capabilities, requestedWidth, requestedHeight);
        var imageCount = SelectImageCount(capabilities);

        var oldSwapchain = _handle;

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = Surface.Handle,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            // ImageUsage: ColorAttachment for rendering, TransferDst for blit from storage image
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = oldSwapchain
        };

        var result = _khrSwapchain.CreateSwapchain(Device, in createInfo, AllocationCallbacks, out var newSwapchain);
        if (result != VkApiResult.Success)
            throw new InvalidOperationException($"Failed to create swapchain: {result}");

        // Cleanup old resources
        DestroyImageViews();
        if (oldSwapchain.Handle != 0)
            _khrSwapchain.DestroySwapchain(Device, oldSwapchain, AllocationCallbacks);

        _handle = newSwapchain;
        ImageFormat = surfaceFormat.Format;
        ColorSpace = surfaceFormat.ColorSpace;
        Extent = extent;

        AcquireSwapchainImages();
        CreateImageViews();

        Log.Debug("Swapchain created: {Width}x{Height}, {Format}, {ImageCount} images",
            extent.Width, extent.Height, surfaceFormat.Format, _images.Length);
    }

    private unsafe void AcquireSwapchainImages()
    {
        uint count = 0;
        _khrSwapchain.GetSwapchainImages(Device, _handle, ref count, null);

        var images = new Image[count];
        fixed (Image* ptr = images)
        {
            _khrSwapchain.GetSwapchainImages(Device, _handle, ref count, ptr);
        }

        _images = new VkImage[count];
        for (var i = 0; i < count; i++)
        {
            _images[i] = new VkImage(
                images[i],
                ImageFormat,
                new Extent3D(Extent.Width, Extent.Height, 1),
                1,
                1,
                ImageType.Type2D,
                ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
                ImageBacking._Swapchain,
                VulkanContext);
        }
    }

    private void CreateImageViews()
    {
        _imageViews = new VkImageView[_images.Length];
        for (var i = 0; i < _images.Length; i++)
        {
            var viewResult = _images[i].CreateView(ImageAspectFlags.ColorBit);
            if (viewResult is not Result<VkImageView, VkApiResult>.Ok(var view))
                throw new InvalidOperationException(
                    $"Failed to create image view: {((Result<VkImageView, VkApiResult>.Error)viewResult).Value}");

            _imageViews[i] = view;
        }
    }

    private void DestroyImageViews()
    {
        foreach (var view in _imageViews)
            view.Dispose();
        _imageViews = [];

        // Mark images as disposed (don't destroy - swapchain owns them)
        foreach (var image in _images)
            image.MarkDisposed();
        _images = [];
    }

    private SurfaceFormatKHR SelectSurfaceFormat(SurfaceFormatKHR[] formats)
    {
        if (formats.Length == 0)
            throw new InvalidOperationException("No surface formats available");

        // Try to find preferred format
        if (_config.PreferredFormat.HasValue)
        {
            var preferred = formats.FirstOrDefault(f =>
                f.Format == _config.PreferredFormat.Value &&
                (!_config.PreferredColorSpace.HasValue || f.ColorSpace == _config.PreferredColorSpace.Value));

            if (preferred.Format != Format.Undefined)
                return preferred;
        }

        // Fallback: B8G8R8A8_UNORM (storage-compatible) with SRGB colorspace.
        // SRGB formats don't support the storage bit, so UNORM is preferred for compute compatibility.
        var unorm = formats.FirstOrDefault(f =>
            f.Format == Format.B8G8R8A8Unorm &&
            f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);

        if (unorm.Format != Format.Undefined)
            return unorm;

        // Try SRGB if UNORM not available
        var srgb = formats.FirstOrDefault(f =>
            f.Format == Format.B8G8R8A8Srgb &&
            f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);

        if (srgb.Format != Format.Undefined)
            return srgb;

        return formats[0];
    }

    private PresentModeKHR SelectPresentMode(PresentModeKHR[] modes)
    {
        if (_config.PreferredPresentMode.HasValue && modes.Contains(_config.PreferredPresentMode.Value))
            return _config.PreferredPresentMode.Value;

        // Mailbox (triple buffering) if available
        if (modes.Contains(PresentModeKHR.MailboxKhr))
            return PresentModeKHR.MailboxKhr;

        // Fifo is always available
        return PresentModeKHR.FifoKhr;
    }

    private static Extent2D SelectExtent(SurfaceCapabilitiesKHR capabilities, uint requestedWidth, uint requestedHeight)
    {
        // If currentExtent is max uint, surface size is determined by swapchain extent
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        var width = Math.Clamp(
            requestedWidth == 0 ? capabilities.CurrentExtent.Width : requestedWidth,
            capabilities.MinImageExtent.Width,
            capabilities.MaxImageExtent.Width);

        var height = Math.Clamp(
            requestedHeight == 0 ? capabilities.CurrentExtent.Height : requestedHeight,
            capabilities.MinImageExtent.Height,
            capabilities.MaxImageExtent.Height);

        return new Extent2D(width, height);
    }

    private uint SelectImageCount(SurfaceCapabilitiesKHR capabilities)
    {
        var count = Math.Max(_config.MinImageCount, capabilities.MinImageCount);

        // MaxImageCount of 0 means unlimited
        if (capabilities.MaxImageCount > 0)
            count = Math.Min(count, capabilities.MaxImageCount);

        return count;
    }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        DestroyImageViews();

        if (_handle.Handle != 0)
        {
            _khrSwapchain.DestroySwapchain(Device, _handle, AllocationCallbacks);
            _handle = default;
        }

        _khrSwapchain.Dispose();
    }
}
