using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace PongMod.Resources;

/// <summary>The compute pass's write view over the shared render target: one <see cref="VkImageView"/> over an N=1 transient leaf that publishes the <c>target</c> moment. Serves as the descriptor's storage-image binding source, contributes the compute-write layout transition as a pre-execute hook, and exposes the backing <see cref="VkImage"/> for the copy pass's read view.</summary>
[PublicAPI]
public sealed class StorageWriteView : IDescriptorBindingSource, IPreExecuteHook, IDisposable
{
    private readonly ImageResource _leaf;
    private readonly VkImageView _view;

    public StorageWriteView(ImageResource leaf, VkImageView view)
    {
        _leaf = leaf;
        _view = view;
    }

    public ImageResource UnderlyingImage => _leaf;

    public VkImage Backing => _leaf.Backing;

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

    /// <summary>Disposes the owned view; the transient leaf backing is manager-owned.</summary>
    public void Dispose() => _view.Dispose();
}
