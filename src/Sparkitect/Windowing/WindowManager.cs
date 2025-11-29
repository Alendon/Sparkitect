using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Windowing;
using Sparkitect.GameState;

namespace Sparkitect.Windowing;

[StateService<IWindowManager, WindowingModule>]
internal class WindowManager : IWindowManager
{
    private IWindow? _window;

    public IWindow? Window => _window;
    public bool IsOpen => _window is { IsClosing: false };

    public void CreateWindow(string title, int width, int height)
    {
        if (_window != null)
        {
            Log.Warning("Window already exists, closing existing window");
            _window.Close();
            _window.Dispose();
        }

        var options = WindowOptions.DefaultVulkan with
        {
            Title = title,
            Size = new(width, height),
            VSync = false,
            WindowBorder = WindowBorder.Resizable
        };

        _window = Silk.NET.Windowing.Window.Create(options);
        _window.Initialize();

        Log.Information("Window created: {Title} ({Width}x{Height})", title, width, height);
    }

    public void PollEvents()
    {
        _window?.DoEvents();
    }

    public void Close()
    {
        if (_window == null) return;

        _window.Close();
        _window.Dispose();
        _window = null;

        Log.Information("Window closed");
    }

    public unsafe IReadOnlyList<string> GetRequiredVulkanExtensions()
    {
        if (_window?.VkSurface == null)
            return [];

        var extensions = _window.VkSurface.GetRequiredExtensions(out var count);
        var result = new List<string>((int)count);

        for (var i = 0; i < count; i++)
        {
            result.Add(SilkMarshal.PtrToString((nint)extensions[i]) ?? string.Empty);
        }

        return result;
    }
}