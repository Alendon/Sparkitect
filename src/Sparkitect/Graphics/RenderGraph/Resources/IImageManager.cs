using JetBrains.Annotations;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public interface IImageManager
{
    void SetSwapchain(VkSwapchain swapchain);

    void InformAcquiredIndex(uint index);

    ImageResource ResolveSwapchainLeaf();
}
