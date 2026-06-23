using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Execute-time payload a bindable view hands the descriptor so the descriptor — not the view —
/// builds the <see cref="WriteDescriptorSet"/>. The union is closed (only StorageImage/StorageBuffer);
/// the <see cref="ToWrite"/> switch has no default arm, so a new unhandled case is a compile error.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record DescriptorBindingPayload
{
    /// <summary>A storage-image binding: an image view bound at a layout.</summary>
    public sealed partial record StorageImage(VkImageView View, ImageLayout Layout) : DescriptorBindingPayload;

    /// <summary>A storage-buffer binding: a buffer range (null <paramref name="Range"/> ⇒ whole size).</summary>
    public sealed partial record StorageBuffer(VkBuffer Buffer, ulong Offset, ulong? Range) : DescriptorBindingPayload;

    /// <summary>
    /// Holds the resource-info struct a <see cref="WriteDescriptorSet"/> points at. The caller owns this
    /// storage and must keep it alive (un-moved) for as long as the produced write is used — push
    /// descriptors read through <see cref="WriteDescriptorSet.PImageInfo"/>/<see cref="WriteDescriptorSet.PBufferInfo"/>
    /// at <c>vkCmdPushDescriptorSet</c> time. Overlapped so a single storage slot serves either case.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WriteInfoStorage
    {
        /// <summary>Image-info slot, set for a <see cref="StorageImage"/> payload.</summary>
        [FieldOffset(0)] public DescriptorImageInfo ImageInfo;

        /// <summary>Buffer-info slot, set for a <see cref="StorageBuffer"/> payload.</summary>
        [FieldOffset(0)] public DescriptorBufferInfo BufferInfo;
    }

    /// <summary>
    /// Build the Vulkan <see cref="WriteDescriptorSet"/> for this payload at <paramref name="binding"/>.
    /// The pointed-to info struct is written into caller-owned <paramref name="storage"/> so the write's
    /// pointer stays valid for the caller's scope (the descriptor pins/scopes it in 54-07).
    /// <see cref="WriteDescriptorSet.DstSet"/> is left default — push descriptors ignore it.
    /// </summary>
    public unsafe WriteDescriptorSet ToWrite(uint binding, ref WriteInfoStorage storage)
    {
        // Capture the storage address up front so the switch arms (which cannot take a `ref`
        // parameter) can point the write at the caller-owned info slot.
        var storagePtr = (WriteInfoStorage*)Unsafe.AsPointer(ref storage);
        return this switch
        {
            StorageImage storageImage => WriteImage(storageImage, binding, storagePtr),
            StorageBuffer storageBuffer => WriteBuffer(storageBuffer, binding, storagePtr),
        };
    }

    private static unsafe WriteDescriptorSet WriteImage(StorageImage payload, uint binding, WriteInfoStorage* storage)
    {
        storage->ImageInfo = new DescriptorImageInfo
        {
            ImageView = payload.View.Handle,
            ImageLayout = payload.Layout,
        };
        return new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            PImageInfo = &storage->ImageInfo,
        };
    }

    private static unsafe WriteDescriptorSet WriteBuffer(StorageBuffer payload, uint binding, WriteInfoStorage* storage)
    {
        storage->BufferInfo = new DescriptorBufferInfo
        {
            Buffer = payload.Buffer.Handle,
            Offset = payload.Offset,
            Range = payload.Range ?? Vk.WholeSize,
        };
        return new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            PBufferInfo = &storage->BufferInfo,
        };
    }
}
