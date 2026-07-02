using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>The transfer-dst swapchain write view (finishline publisher): a thin composite over the swapchain leaf that contributes both its transfer-dst (pre-execute) and present (finishline) transitions as lifecycle hooks, and exposes the backing <see cref="VkImage"/> as the blit destination.</summary>
[PublicAPI]
public sealed class SwapchainWriteView : IPreExecuteHook, IFinishlineHook
{
    private readonly ImageResource _leaf;

    /// <summary>Composes the swapchain <paramref name="leaf"/> as the frame's transfer-dst and finishline publisher.</summary>
    public SwapchainWriteView(ImageResource leaf) => _leaf = leaf;

    /// <summary>The backing swapchain image, exposed as the blit destination.</summary>
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
