using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sundew.DiscriminatedUnions;

namespace PongMod.Resources;

/// <summary>Execute-time payload a bindable view hands the descriptor to build a write; a closed union whose <see cref="ToWrite"/> switch has no default arm, so an unhandled case is a compile error.</summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record DescriptorBindingPayload
{
    public sealed partial record StorageImage(VkImageView View, ImageLayout Layout) : DescriptorBindingPayload;

    /// <summary>Holds the resource-info struct a write points at; the caller must keep it alive until the write is consumed, since push descriptors read through its pointer at push time.</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WriteInfoStorage
    {
        [FieldOffset(0)] public DescriptorImageInfo ImageInfo;
    }

    /// <summary>Builds the write at <paramref name="binding"/>, pointing it at caller-owned <paramref name="storage"/> so the pointer stays valid for the caller's scope.</summary>
    public unsafe WriteDescriptorSet ToWrite(uint binding, ref WriteInfoStorage storage)
    {
        // Switch arms can't take a ref param, so capture the storage address up front.
        var storagePtr = (WriteInfoStorage*)Unsafe.AsPointer(ref storage);
        return this switch
        {
            StorageImage storageImage => WriteImage(storageImage, binding, storagePtr),
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
}
