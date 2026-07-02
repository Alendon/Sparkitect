using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;

namespace Sparkitect.Windowing;

/// <summary>
/// Creates and tracks application windows and reports the Vulkan instance extensions the windowing
/// backend requires.
/// </summary>
[StateFacade<IWindowManagerStateFacade>]
[PublicAPI]
public interface IWindowManager
{
    /// <summary>Creates a window with the given title, size, and optional swapchain configuration.</summary>
    /// <param name="title">Initial window title.</param>
    /// <param name="width">Initial client-area width in pixels.</param>
    /// <param name="height">Initial client-area height in pixels.</param>
    /// <param name="config">Optional swapchain configuration; defaults are used when null.</param>
    ISparkitWindow CreateWindow(string title, int width, int height, SwapchainConfig? config = null);

    /// <summary>The Vulkan instance extensions the windowing backend requires.</summary>
    IReadOnlyList<string> GetRequiredVulkanExtensions();
}

/// <summary>State-facade marker for <see cref="IWindowManager"/>.</summary>
[FacadeFor<IWindowManager>]
[PublicAPI]
public interface IWindowManagerStateFacade;
