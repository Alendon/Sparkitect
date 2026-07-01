using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Read-usage view over the published entity-list composite: resolves the same N=1
/// <see cref="EntityListResource"/> instance the staging pass published (via the <c>entities_gpu</c> moment)
/// and exposes its element <see cref="Count"/> and device <see cref="Buffer"/> for the consuming compute
/// pass — the count read off the composite, never through DI. As a pass root it contributes the device
/// buffer's transfer-&gt;compute barrier as a pre-execute hook, so the compute pass records no manual sync,
/// and it serves as the storage-buffer binding value the compute descriptor binds at its buffer slot.
/// </summary>
[PublicAPI]
public sealed class EntityListReadView : IPreExecuteHook, IDescriptorValue
{
    private readonly EntityListResource _composite;

    /// <summary>Composes the read view over the resolved published composite.</summary>
    public EntityListReadView(EntityListResource composite) => _composite = composite;

    /// <summary>The element count, materialized on the composite at the producing pass's Execute.</summary>
    public int Count => _composite.Count;

    /// <summary>The device buffer backing the entity list — the compute pass's shader-read storage buffer.</summary>
    public BufferResource Buffer => _composite.Buffer;

    /// <summary>Descriptor type read at Setup for layout derivation: the entity list binds as a storage buffer.</summary>
    public DescriptorType DescriptorType => Silk.NET.Vulkan.DescriptorType.StorageBuffer;

    /// <inheritdoc/>
    public DescriptorBindingPayload DescribeBinding() =>
        new DescriptorBindingPayload.StorageBuffer(_composite.Buffer.Backing, 0, null);

    /// <summary>
    /// Pre-execute hook: reconcile the device buffer from transfer-write to compute shader-read before the
    /// compute pass dispatches (the transfer-&gt;compute barrier the deprecated staging pass recorded inline).
    /// </summary>
    public void PreExecute(VkCommandBuffer commandBuffer) =>
        _composite.Buffer.BarrierTo(
            commandBuffer,
            PipelineStageFlags.ComputeShaderBit,
            AccessFlags.ShaderReadBit);
}
