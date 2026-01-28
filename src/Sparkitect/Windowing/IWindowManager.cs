using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;

namespace Sparkitect.Windowing;

[StateFacade<IWindowManager>]
public interface IWindowManager
{
    ISparkitWindow? MainWindow { get; set; }

    ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null);

    IReadOnlyList<string> GetRequiredVulkanExtensions();
}

public interface IWindowManagerStateFacade;
