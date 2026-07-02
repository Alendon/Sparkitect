using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


/// <summary>Graph-local provider of image leaves: the swapchain-origin leaf for the current acquired index and a reused VMA-transient leaf.</summary>
[PublicAPI]
public interface IImageManager
{
    /// <summary>Applies the swapchain the swapchain-origin leaves resolve from; call before resolving one.</summary>
    void SetSwapchain(VkSwapchain swapchain);

    /// <summary>Sets the acquired swapchain image <paramref name="index"/> the next swapchain-leaf resolve reads.</summary>
    void InformAcquiredIndex(uint index);

    /// <summary>Resolves a single-index leaf over the current frame's acquired swapchain image.</summary>
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
