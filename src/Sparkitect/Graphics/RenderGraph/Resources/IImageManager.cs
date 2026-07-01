using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public interface IImageManager
{
    void SetSwapchain(VkSwapchain swapchain);

    void InformAcquiredIndex(uint index);

    ImageResource ResolveSwapchainLeaf();

    /// <summary>
    /// Resolves a single (N=1) VMA-transient image leaf sized from <paramref name="intent"/> in
    /// <paramref name="format"/>. The <see cref="ExtentIntent"/> is resolved to a concrete extent at
    /// call time (e.g. <see cref="ExtentIntent.MatchSwapchain"/> ⇒ the applied swapchain's extent). The
    /// backing is allocated once and the same leaf is returned on subsequent resolves within the graph
    /// lifetime; it is not swapchain-indexed.
    /// </summary>
    ImageResource ResolveTransientLeaf(ExtentIntent intent, Format format);
}
