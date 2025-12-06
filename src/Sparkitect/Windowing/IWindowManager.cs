using Sparkitect.Graphics.Vulkan;

namespace Sparkitect.Windowing;

public interface IWindowManager
{
    ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null);

    IReadOnlyList<string> GetRequiredVulkanExtensions();
}
