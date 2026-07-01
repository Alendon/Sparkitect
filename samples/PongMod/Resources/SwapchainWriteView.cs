using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>
/// The copy pass's transfer-dst swapchain write view — the finishline publisher (D-03) and the transfer-dst
/// side of the deprecated <c>WriteableImage</c>. It adopts the per-frame swapchain leaf and carries its
/// tracked state, contributing BOTH of its synchronization transitions as lifecycle hooks: the transfer-dst
/// transition before the copy blits into it (pre-execute) and the present transition after all passes
/// (finishline, D-09). The render graph type-casts this instance to the hook interfaces and dispatches them
/// at the plan-positioned points; nothing imperative remains in the copy pass or frame loop.
/// </summary>
/// <remarks>
/// It exposes the underlying <see cref="VkImage"/> (via <see cref="ImageResource.Backing"/>) as the blit
/// destination for <c>vkCmdBlitImage</c>.
/// </remarks>
[PublicAPI]
public sealed class SwapchainWriteView : ImageResource, IPreExecuteHook, IFinishlineHook
{
    /// <summary>Adopts the resolved swapchain <paramref name="leaf"/>'s backing and initial tracked state.</summary>
    public SwapchainWriteView(ImageResource leaf)
        : base(leaf.Backing, leaf.Extent, leaf.Format, leaf.CurrentLayout, leaf.CurrentAccess)
    {
    }

    /// <summary>Pre-execute hook: reconcile to transfer-dst so the copy can blit into the swapchain image.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        TransitionTo(
            commandBuffer,
            ImageLayout.TransferDstOptimal,
            AccessFlags.TransferWriteBit,
            PipelineStageFlags.TransferBit);

    /// <summary>Finishline hook: reconcile to the present layout after all passes have recorded (D-09).</summary>
    public void OnFinishline(VkCommandBuffer commandBuffer) =>
        TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            AccessFlags.MemoryReadBit,
            PipelineStageFlags.BottomOfPipeBit);
}
