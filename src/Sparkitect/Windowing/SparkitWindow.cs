using Serilog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Windowing.Input;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Sparkitect.Windowing;

internal class SparkitWindow : ISparkitWindow
{
    private readonly IWindow _silkWindow;
    private readonly VkSurface _surface;
    private readonly VkSwapchain _swapchain;
    private readonly IInputContext _inputContext;
    private readonly SparkitKeyboard _keyboard;
    private readonly SparkitMouse _mouse;
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

    public Input.IKeyboard Keyboard => _keyboard;
    public IMouseInput Mouse => _mouse;

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

        _inputContext = silkWindow.CreateInput();

        if (_inputContext.Keyboards.Count == 0)
            throw new InvalidOperationException("No keyboard available");
        if (_inputContext.Mice.Count == 0)
            throw new InvalidOperationException("No mouse available");

        _keyboard = new SparkitKeyboard(_inputContext.Keyboards[0]);
        _mouse = new SparkitMouse(_inputContext.Mice[0]);

        _silkWindow.Resize += OnWindowResize;
        _silkWindow.FocusChanged += OnFocusChanged;
    }

    public void PollEvents()
    {
        _silkWindow.DoEvents();
        _mouse.UpdateDelta();
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

    private void OnFocusChanged(bool focused)
    {
        _keyboard.SetFocusState(focused);
        _mouse.SetFocusState(focused);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _silkWindow.Resize -= OnWindowResize;
        _silkWindow.FocusChanged -= OnFocusChanged;

        _inputContext.Dispose();

        _swapchain.Dispose();
        _surface.Dispose();

        _silkWindow.Close();
        _silkWindow.Dispose();

        Log.Information("SparkitWindow disposed");
    }
}
