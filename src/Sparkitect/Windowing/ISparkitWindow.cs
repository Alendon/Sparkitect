using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Windowing.Input;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace Sparkitect.Windowing;

[PublicAPI]
public interface ISparkitWindow : IDisposable
{
    IWindow SilkWindow { get; }

    int Width { get; }
    int Height { get; }
    string Title { get; set; }
    bool IsOpen { get; }

    IKeyboard Keyboard { get; }
    IMouseInput Mouse { get; }

    VkSurface Surface { get; }
    VkSwapchain Swapchain { get; }

    void PollEvents();

    VkResult<uint> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false);

    Result Present(uint imageIndex, VkSemaphore waitSemaphore, Queue presentQueue);
}
