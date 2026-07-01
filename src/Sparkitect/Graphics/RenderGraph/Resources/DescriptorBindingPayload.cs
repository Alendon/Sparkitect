using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Execute-time payload a bindable view hands the descriptor to build a write; a closed union (image/buffer) whose <see cref="ToWrite"/> switch has no default arm, so an unhandled case is a compile error.</summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record DescriptorBindingPayload
{
    /// <summary>A storage-image binding: an image view bound at a layout.</summary>
    public sealed partial record StorageImage(VkImageView View, ImageLayout Layout) : DescriptorBindingPayload;

    /// <summary>A storage-buffer binding: a buffer range (null <paramref name="Range"/> ⇒ whole size).</summary>
    public sealed partial record StorageBuffer(VkBuffer Buffer, ulong Offset, ulong? Range) : DescriptorBindingPayload;

    /// <summary>Holds the resource-info struct a write points at; the caller must keep it alive until the write is consumed, since push descriptors read through its pointer at push time. Overlapped so one slot serves either arm.</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WriteInfoStorage
    {
        [FieldOffset(0)] public DescriptorImageInfo ImageInfo;
        [FieldOffset(0)] public DescriptorBufferInfo BufferInfo;
    }

    /// <summary>Builds the write at <paramref name="binding"/>, pointing it at caller-owned <paramref name="storage"/> so the pointer stays valid for the caller's scope.</summary>
    public unsafe WriteDescriptorSet ToWrite(uint binding, ref WriteInfoStorage storage)
    {
        // Switch arms can't take a ref param, so capture the storage address up front.
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
