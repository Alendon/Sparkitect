using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>
/// The copy pass's transfer-dst swapchain write view — the finishline publisher and the transfer-dst side
/// of the deprecated <c>WriteableImage</c>. It is a thin composite over the sub-declared swapchain leaf: it
/// holds the leaf by reference and contributes BOTH of its synchronization transitions as lifecycle hooks,
/// delegating each to the leaf — the transfer-dst transition before the copy blits into it (pre-execute)
/// and the present transition after all passes (finishline). It carries no state of its own; the render
/// graph type-casts this instance to the hook interfaces and dispatches them at the plan-positioned points.
/// </summary>
/// <remarks>
/// It exposes the leaf's underlying <see cref="VkImage"/> (via <see cref="Backing"/>) as the blit
/// destination for <c>vkCmdBlitImage</c>.
/// </remarks>
[PublicAPI]
public sealed class SwapchainWriteView : IPreExecuteHook, IFinishlineHook
{
    private readonly ImageResource _leaf;

    /// <summary>Composes over the resolved swapchain <paramref name="leaf"/> by reference.</summary>
    public SwapchainWriteView(ImageResource leaf) => _leaf = leaf;

    /// <summary>The shared swapchain backing — the blit destination for the copy pass.</summary>
    public VkImage Backing => _leaf.Backing;

    /// <summary>Pre-execute hook: reconcile the leaf to transfer-dst so the copy can blit into it.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.TransferDstOptimal,
            AccessFlags.TransferWriteBit,
            PipelineStageFlags.TransferBit);

    /// <summary>Finishline hook: reconcile the leaf to the present layout after all passes have recorded.</summary>
    public void OnFinishline(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            AccessFlags.MemoryReadBit,
            PipelineStageFlags.BottomOfPipeBit);
}
