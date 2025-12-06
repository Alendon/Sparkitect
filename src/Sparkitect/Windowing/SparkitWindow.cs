using Serilog;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Sparkitect.Windowing;

internal class SparkitWindow : ISparkitWindow
{
    private readonly IWindow _silkWindow;
    private readonly VkSurface _surface;
    private readonly VkSwapchain _swapchain;
    private bool _isDisposed;

    public IWindow SilkWindow => _silkWindow;

    public int Width => _silkWindow.Size.X;
    public int Height => _silkWindow.Size.Y;

    public string Title
    {
        get => _silkWindow.Title;
        set => _silkWindow.Title = value;
    }

    public bool IsOpen => !_silkWindow.IsClosing && !_isDisposed;

    public VkSurface Surface => _surface;
    public VkSwapchain Swapchain => _swapchain;

    internal SparkitWindow(
        IWindow silkWindow,
        VkSurface surface,
        VkSwapchain swapchain)
    {
        _silkWindow = silkWindow;
        _surface = surface;
        _swapchain = swapchain;

        _silkWindow.Resize += OnWindowResize;
    }

    public void PollEvents()
    {
        _silkWindow.DoEvents();
    }

    public VkResult<uint> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false)
    {
        return _swapchain.AcquireNextImage(signalSemaphore, timeout, autoRecreate);
    }

    public Result Present(uint imageIndex, VkSemaphore waitSemaphore, Queue presentQueue)
    {
        return _swapchain.Present(imageIndex, waitSemaphore, presentQueue);
    }

    private void OnWindowResize(Vector2D<int> newSize)
    {
        if (newSize.X <= 0 || newSize.Y <= 0)
        {
            Log.Debug("Window minimized, skipping swapchain recreation");
            return;
        }

        Log.Debug("Window resized to {Width}x{Height}, recreating swapchain", newSize.X, newSize.Y);
        _swapchain.Recreate((uint)newSize.X, (uint)newSize.Y);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _silkWindow.Resize -= OnWindowResize;

        _swapchain.Dispose();
        _surface.Dispose();

        _silkWindow.Close();
        _silkWindow.Dispose();

        Log.Information("SparkitWindow disposed");
    }
}
