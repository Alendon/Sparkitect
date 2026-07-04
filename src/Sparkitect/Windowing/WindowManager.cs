using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.CompilerGenerated;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Settings;

namespace Sparkitect.Windowing;

[StateService<IWindowManager, WindowingModule>]
internal class WindowManager : IWindowManager, IWindowManagerStateFacade
{
    internal required IVulkanContext VulkanContext { private get; init; }

    /// <summary>The settings manager; window size and vsync/present-mode are read inline from it (D-17).</summary>
    internal required ISettingsManager SettingsManager { private get; init; }

    /// <summary>
    /// Creates a window sized from the <c>window_width</c>/<c>window_height</c> settings and configured with
    /// the <c>vsync</c> setting's present mode (FIFO when on, otherwise the default). The settings supply the
    /// values a game window would otherwise hardcode; callers pass only a title.
    /// </summary>
    public ISparkitWindow CreateGameWindow(string title)
    {
        var width = SettingsManager.Window.Width.Value;
        var height = SettingsManager.Window.Height.Value;
        var vsync = SettingsManager.Graphics.VSync.Value;

        var config = SwapchainConfig.Default with
        {
            PreferredPresentMode = vsync ? PresentModeKHR.FifoKhr : SwapchainConfig.Default.PreferredPresentMode,
        };

        return CreateWindow(title, width, height, config);
    }

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
        var options = new WindowOptions(ViewOptions.DefaultVulkan)
        {
            Size = new Vector2D<int>(1, 1),
            Title = "Extension Query",
            IsVisible = false,
        };
        var tempWindow = Window.Create(options);
        tempWindow.Initialize();

        try
        {
            if (tempWindow.VkSurface == null)
                return [];

            var extensions = tempWindow.VkSurface.GetRequiredExtensions(out var count);
            var result = new List<string>((int)count);

            for (var i = 0; i < count; i++)
            {
                result.Add(SilkMarshal.PtrToString((nint)extensions[i]) ?? string.Empty);
            }

            return result;
        }
        finally
        {
            tempWindow.Close();
            tempWindow.Dispose();
        }
    }
}
