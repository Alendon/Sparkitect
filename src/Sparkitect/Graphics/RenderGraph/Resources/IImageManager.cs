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
    /// Resolves a VMA-transient image leaf sized from <paramref name="intent"/> in
    /// <paramref name="format"/>. The backing is allocated once and reused for the graph's lifetime; it is not swapchain-indexed.
    /// </summary>
    ImageResource ResolveTransientLeaf(ExtentIntent intent, Format format);

    /// <summary>
    /// Frees the manager-owned transient backing at graph teardown (the <see cref="Sparkitect.Graphing.Descriptions.CleanupStrategy.Release"/>
    /// path for the VMA transient). Swapchain leaves are owned by the swapchain and are not freed here.
    /// </summary>
    void DisposeTransient();
}
