using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sundew.DiscriminatedUnions;

namespace PongMod.Resources;

/// <summary>
/// Execute-time payload a bindable view hands the descriptor so the descriptor — not the view —
/// builds the <see cref="WriteDescriptorSet"/>. The union is closed (only <see cref="StorageImage"/>
/// for now — StorageBuffer arrives with the Space Invaders migration); the <see cref="ToWrite"/> switch
/// has no default arm, so a new unhandled case is a compile error.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record DescriptorBindingPayload
{
    /// <summary>A storage-image binding: an image view bound at a layout.</summary>
    public sealed partial record StorageImage(VkImageView View, ImageLayout Layout) : DescriptorBindingPayload;

    /// <summary>
    /// Holds the resource-info struct a <see cref="WriteDescriptorSet"/> points at. The caller owns this
    /// storage and must keep it alive (un-moved) for as long as the produced write is used — push
    /// descriptors read through <see cref="WriteDescriptorSet.PImageInfo"/> at
    /// <c>vkCmdPushDescriptorSet</c> time.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct WriteInfoStorage
    {
        /// <summary>Image-info slot, set for a <see cref="StorageImage"/> payload.</summary>
        [FieldOffset(0)] public DescriptorImageInfo ImageInfo;
    }

    /// <summary>
    /// Build the Vulkan <see cref="WriteDescriptorSet"/> for this payload at <paramref name="binding"/>.
    /// The pointed-to info struct is written into caller-owned <paramref name="storage"/> so the write's
    /// pointer stays valid for the caller's scope (the descriptor pins/scopes it during its push).
    /// <see cref="WriteDescriptorSet.DstSet"/> is left default — push descriptors ignore it.
    /// </summary>
    public unsafe WriteDescriptorSet ToWrite(uint binding, ref WriteInfoStorage storage)
    {
        // Capture the storage address up front so the switch arm (which cannot take a `ref`
        // parameter) can point the write at the caller-owned info slot.
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
