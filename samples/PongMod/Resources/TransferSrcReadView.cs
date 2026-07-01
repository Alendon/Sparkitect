using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>The copy pass's transfer-src read view over the shared target: layout-only, composing the same N=1 transient leaf the write view published so both share one tracked layout state. Contributes the transfer-src layout transition as a pre-execute hook and exposes the backing <see cref="VkImage"/> as the blit source.</summary>
[PublicAPI]
public sealed class TransferSrcReadView : IPreExecuteHook
{
    private readonly ImageResource _leaf;

    public TransferSrcReadView(ImageResource leaf) => _leaf = leaf;

    public ImageResource UnderlyingImage => _leaf;

    public VkImage Backing => _leaf.Backing;

    /// <summary>Pre-execute hook: reconcile the shared leaf to transfer-src before the copy blits from it.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.TransferSrcOptimal,
            AccessFlags.TransferReadBit,
            PipelineStageFlags.TransferBit);
}
