using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Windowing;

[StateService<IWindowManager, WindowingModule>]
internal class WindowManager : IWindowManager
{
    private IWindow? _preInitWindow;

    internal required IVulkanContext VulkanContext { private get; init; }

    public ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null)
    {
        var options = new WindowOptions(ViewOptions.DefaultVulkan)
        {
            Size = new Vector2D<int>(width, height),
            Title = title,
        };

        var silkWindow = Window.Create(options);
        silkWindow.Initialize();

        var surface = VulkanContext.CreateSurface(silkWindow)
            ?? throw new InvalidOperationException("Failed to create Vulkan surface");

        var swapchain = new VkSwapchain(surface, config ?? SwapchainConfig.Default, VulkanContext, (uint)width, (uint)height);

        var window = new SparkitWindow(silkWindow, surface, swapchain);

        Log.Information("Window created: {Title} ({Width}x{Height})", title, width, height);

        return window;
    }

    public unsafe IReadOnlyList<string> GetRequiredVulkanExtensions()
    {
        if (_preInitWindow == null)
        {
            var options = new WindowOptions(ViewOptions.DefaultVulkan)
            {
                Size = new Vector2D<int>(1, 1),
                Title = "Extension Query",
                IsVisible = false,
            };
            _preInitWindow = Window.Create(options);
            _preInitWindow.Initialize();
        }

        if (_preInitWindow.VkSurface == null)
            return [];

        var extensions = _preInitWindow.VkSurface.GetRequiredExtensions(out var count);
        var result = new List<string>((int)count);

        for (var i = 0; i < count; i++)
        {
            result.Add(SilkMarshal.PtrToString((nint)extensions[i]) ?? string.Empty);
        }

        return result;
    }
}
