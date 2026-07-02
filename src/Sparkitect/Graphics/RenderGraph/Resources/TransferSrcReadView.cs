using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>A transfer-src read view over a shared target: layout-only, composing the same N=1 transient leaf a write view published so both share one tracked layout state. Contributes the transfer-src layout transition as a pre-execute hook and exposes the backing <see cref="VkImage"/> as the blit source.</summary>
[PublicAPI]
public sealed class TransferSrcReadView : IPreExecuteHook
{
    private readonly ImageResource _leaf;

    /// <summary>Composes the same shared transient <paramref name="leaf"/> a write view published so both track one layout state.</summary>
    public TransferSrcReadView(ImageResource leaf) => _leaf = leaf;

    /// <summary>The shared transient leaf this view reads.</summary>
    public ImageResource UnderlyingImage => _leaf;

    /// <summary>The backing image, exposed as the blit source.</summary>
    public VkImage Backing => _leaf.Backing;

    /// <summary>Pre-execute hook: reconcile the shared leaf to transfer-src before the copy blits from it.</summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.TransferSrcOptimal,
            AccessFlags.TransferReadBit,
            PipelineStageFlags.TransferBit);
}
