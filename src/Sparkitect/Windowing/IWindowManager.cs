using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;

namespace Sparkitect.Windowing;

[StateFacade<IWindowManagerStateFacade>]
[PublicAPI]
public interface IWindowManager
{
    ISparkitWindow? MainWindow { get; set; }

    ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null);

    IReadOnlyList<string> GetRequiredVulkanExtensions();
}

[FacadeFor<IWindowManager>]
[PublicAPI]
public interface IWindowManagerStateFacade;
