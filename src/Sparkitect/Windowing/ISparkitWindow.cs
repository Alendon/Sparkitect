using JetBrains.Annotations;
using Silk.NET.Windowing;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing.Input;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Windowing;

/// <summary>
/// A window with its Vulkan surface and swapchain. Owns input state and the present loop for one
/// on-screen render target.
/// </summary>
[PublicAPI]
public interface ISparkitWindow : IDisposable
{
    /// <summary>The underlying Silk.NET window.</summary>
    IWindow SilkWindow { get; }

    /// <summary>Current client-area width in pixels.</summary>
    int Width { get; }

    /// <summary>Current client-area height in pixels.</summary>
    int Height { get; }

    /// <summary>The window title bar text.</summary>
    string Title { get; set; }

    /// <summary>True while the window is open and has not been requested to close.</summary>
    bool IsOpen { get; }

    /// <summary>Keyboard input for this window.</summary>
    IKeyboard Keyboard { get; }

    /// <summary>Mouse input for this window.</summary>
    IMouseInput Mouse { get; }

    /// <summary>The Vulkan surface this window presents to.</summary>
    VkSurface Surface { get; }

    /// <summary>The swapchain backing this window's presentation.</summary>
    VkSwapchain Swapchain { get; }

    /// <summary>Pumps pending window and input events.</summary>
    void PollEvents();

    /// <summary>
    /// Acquires the next swapchain image, signalling <paramref name="signalSemaphore"/> when it is ready.
    /// Returns the image index on success or the Vulkan result on failure.
    /// </summary>
    /// <param name="signalSemaphore">Semaphore signalled once the image is available.</param>
    /// <param name="timeout">Acquire timeout in nanoseconds.</param>
    /// <param name="autoRecreate">Recreate the swapchain automatically when out-of-date.</param>
    Result<uint, VkApiResult> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false);

    /// <summary>Presents the acquired image after <paramref name="waitSemaphore"/> signals.</summary>
    /// <param name="imageIndex">Swapchain image index to present.</param>
    /// <param name="waitSemaphore">Semaphore waited on before presenting.</param>
    /// <param name="presentQueue">Queue used to submit the present.</param>
    VkApiResult Present(uint imageIndex, VkSemaphore waitSemaphore, VkQueue presentQueue);
}
