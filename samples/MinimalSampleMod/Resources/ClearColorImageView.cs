using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace MinimalSampleMod.Resources;

/// <summary>
/// Thin clear-color view over the swapchain leaf. It carries the leaf's tracked state and contributes
/// both of its synchronization transitions as lifecycle hooks: the transfer-dst transition before the
/// pass clears it (pre-execute) and the present transition after all passes (finishline). The render
/// graph type-casts this instance to the hook interfaces and dispatches them at the plan-positioned
/// points; the view itself issues no work outside those hook bodies.
/// </summary>
[PublicAPI]
public sealed class ClearColorImageView : ImageResource, IPreExecuteHook, IFinishlineHook
{
    /// <summary>Adopts the resolved swapchain leaf's backing and initial tracked state.</summary>
    public ClearColorImageView(ImageResource leaf)
        : base(leaf.Backing, leaf.Extent, leaf.Format, leaf.CurrentLayout, leaf.CurrentAccess)
    {
    }

    /// <summary>Pre-execute hook: reconcile to transfer-dst so the pass can clear the image.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        TransitionTo(
            commandBuffer,
            ImageLayout.TransferDstOptimal,
            AccessFlags.TransferWriteBit,
            PipelineStageFlags.TransferBit);

    /// <summary>Finishline hook: reconcile to the present layout after all passes have recorded.</summary>
    public void OnFinishline(VkCommandBuffer commandBuffer) =>
        TransitionTo(
            commandBuffer,
            ImageLayout.PresentSrcKhr,
            AccessFlags.MemoryReadBit,
            PipelineStageFlags.BottomOfPipeBit);
}
