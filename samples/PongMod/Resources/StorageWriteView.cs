using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>
/// The compute pass's single write view over the shared render target (D-14 — collapses the deprecated
/// <c>WriteableImage</c> + <c>StorageImageView</c> into one composite). It owns one
/// <see cref="VkImageView"/> over a VMA-transient leaf resolved once (N=1) from the image manager, and
/// is the resource that publishes the <c>target</c> moment.
/// </summary>
/// <remarks>
/// It plays three roles: it is the descriptor's storage-image binding source (static
/// <see cref="DescriptorType"/> for Setup-time layout derivation + <see cref="DescribeBinding"/> at
/// Execute); it contributes the compute-write layout transition as a pre-execute hook (the RG dispatches
/// it — no pass-invoked <c>PreExecute</c>); and it exposes the underlying <see cref="VkImage"/> so the
/// copy pass's read view can reach the same backing as a blit source.
/// </remarks>
[PublicAPI]
public sealed class StorageWriteView : IDescriptorBindingSource, IPreExecuteHook, IDisposable
{
    private readonly ImageResource _leaf;
    private readonly VkImageView _view;

    /// <summary>Wraps the manager-owned transient <paramref name="leaf"/> and the view-owned <paramref name="view"/>.</summary>
    public StorageWriteView(ImageResource leaf, VkImageView view)
    {
        _leaf = leaf;
        _view = view;
    }

    /// <summary>The underlying transient leaf (the shared target backing).</summary>
    public ImageResource UnderlyingImage => _leaf;

    /// <summary>The shared backing image — the blit source for the copy pass's read view.</summary>
    public VkImage Backing => _leaf.Backing;

    /// <summary>The storage image view bound into the descriptor.</summary>
    public VkImageView View => _view;

    /// <summary>Static descriptor type read at Setup for layout derivation off this concrete view type.</summary>
    public static DescriptorType DescriptorType => Silk.NET.Vulkan.DescriptorType.StorageImage;

    /// <inheritdoc/>
    public DescriptorBindingPayload DescribeBinding() =>
        new DescriptorBindingPayload.StorageImage(_view, ImageLayout.General);

    /// <inheritdoc/>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _leaf.TransitionTo(
            commandBuffer,
            ImageLayout.General,
            AccessFlags.ShaderWriteBit,
            PipelineStageFlags.ComputeShaderBit);

    /// <summary>Disposes the owned <see cref="VkImageView"/>; the transient leaf backing is manager-owned.</summary>
    public void Dispose() => _view.Dispose();
}
