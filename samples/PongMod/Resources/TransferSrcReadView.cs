using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>
/// The copy pass's transfer-src read view over the shared target (D-14 — the near-exact port of the
/// deprecated <c>ReadableImage</c>). Layout-only: it owns no <see cref="VkImageView"/> of its own and
/// composes the same manager-owned N=1 transient leaf the compute write view published, so both views
/// share one tracked layout state across passes.
/// </summary>
/// <remarks>
/// It contributes the transfer-src layout transition as a pre-execute hook (the RG dispatches it — no
/// pass-invoked <c>PreExecute</c>) reconciling the leaf to <see cref="ImageLayout.TransferSrcOptimal"/>,
/// and exposes the underlying <see cref="VkImage"/> as the blit source for <c>vkCmdBlitImage</c>.
/// </remarks>
[PublicAPI]
public sealed class TransferSrcReadView : IPreExecuteHook
{
    private readonly ImageResource _leaf;

    /// <summary>Wraps the shared manager-owned transient <paramref name="leaf"/> (the target backing).</summary>
    public TransferSrcReadView(ImageResource leaf) => _leaf = leaf;

    /// <summary>The underlying transient leaf — the same shared instance the write view carries.</summary>
    public ImageResource UnderlyingImage => _leaf;

    /// <summary>The shared backing image — the blit source for the copy pass.</summary>
    public VkImage Backing => _leaf.Backing;

    /// <summary>Pre-execute hook: reconcile the shared leaf to transfer-src before the copy blits from it.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.TransferSrcOptimal,
            AccessFlags.TransferReadBit,
            PipelineStageFlags.TransferBit);
}
