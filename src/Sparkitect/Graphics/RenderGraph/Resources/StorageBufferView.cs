using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Descriptor-bindable storage-buffer view over a device-local <see cref="VkBuffer"/> backing.
/// Unlike the image view, a storage buffer has no view object, so this view owns no Vulkan
/// resource of its own and is not disposable. Implements <see cref="IDescriptorBindingSource"/>
/// so the descriptor — not the view — builds the <see cref="WriteDescriptorSet"/>: the static
/// <see cref="DescriptorType"/> is read at Setup for layout derivation, and
/// <see cref="DescribeBinding"/> hands the descriptor a
/// <see cref="DescriptorBindingPayload.StorageBuffer"/> at Execute.
/// </summary>
/// <remarks>
/// The backing <see cref="VkBuffer"/> is exposed through the public <see cref="Buffer"/> accessor:
/// the buffer family is public by construction, so a mod-assembly staging pass can fetch a declared
/// device-buffer handle and unwrap it to the raw buffer for a buffer-to-buffer copy.
/// </remarks>
[ResourceManager<BufferResourceManager>]
[GraphResourceRegistry.RegisterResource("storage_buffer_view")]
[PublicAPI]
public sealed partial class StorageBufferView : IHasIdentification, IDescriptorBindingSource
{
    private readonly VkBuffer _buffer;

    internal StorageBufferView(VkBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <summary>The device-local buffer backing, reachable cross-assembly for a buffer-to-buffer copy.</summary>
    public VkBuffer Buffer => _buffer;

    public DescriptorType DescriptorType => DescriptorType.StorageBuffer;

    public DescriptorBindingPayload DescribeBinding() =>
        new DescriptorBindingPayload.StorageBuffer(_buffer, 0, null);
}
