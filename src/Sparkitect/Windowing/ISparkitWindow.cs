using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing.Input;
using VkApiResult = Silk.NET.Vulkan.Result;

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

    Result<uint, VkApiResult> AcquireNextImage(
        VkSemaphore signalSemaphore,
        ulong timeout = ulong.MaxValue,
        bool autoRecreate = false);

    VkApiResult Present(uint imageIndex, VkSemaphore waitSemaphore, Queue presentQueue);
}
