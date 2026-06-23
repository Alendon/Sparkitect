using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Tests.RenderGraph;

public class DescriptorBindingPayloadTests
{
    private static VkImageView UninitImageView() =>
        (VkImageView)RuntimeHelpers.GetUninitializedObject(typeof(VkImageView));

    private static VkBuffer UninitBuffer() =>
        (VkBuffer)RuntimeHelpers.GetUninitializedObject(typeof(VkBuffer));

    [Test]
    public async Task StorageImage_ToWrite_MapsNonPointerFields()
    {
        var payload = new DescriptorBindingPayload.StorageImage(UninitImageView(), ImageLayout.General);
        var storage = default(DescriptorBindingPayload.WriteInfoStorage);

        var write = payload.ToWrite(3, ref storage);

        await Assert.That(write.SType).IsEqualTo(StructureType.WriteDescriptorSet);
        await Assert.That(write.DescriptorType).IsEqualTo(DescriptorType.StorageImage);
        await Assert.That(write.DstBinding).IsEqualTo(3u);
        await Assert.That(write.DstArrayElement).IsEqualTo(0u);
        await Assert.That(write.DescriptorCount).IsEqualTo(1u);
        await Assert.That(write.DstSet).IsEqualTo(default(DescriptorSet));
    }

    [Test]
    public async Task StorageBuffer_ToWrite_MapsNonPointerFields()
    {
        var payload = new DescriptorBindingPayload.StorageBuffer(UninitBuffer(), Offset: 0, Range: null);
        var storage = default(DescriptorBindingPayload.WriteInfoStorage);

        var write = payload.ToWrite(5, ref storage);

        await Assert.That(write.SType).IsEqualTo(StructureType.WriteDescriptorSet);
        await Assert.That(write.DescriptorType).IsEqualTo(DescriptorType.StorageBuffer);
        await Assert.That(write.DstBinding).IsEqualTo(5u);
        await Assert.That(write.DstArrayElement).IsEqualTo(0u);
        await Assert.That(write.DescriptorCount).IsEqualTo(1u);
        await Assert.That(write.DstSet).IsEqualTo(default(DescriptorSet));
    }

    [Test]
    public async Task StorageBuffer_ToWrite_NullRange_FillsWholeSizeInStorage()
    {
        var payload = new DescriptorBindingPayload.StorageBuffer(UninitBuffer(), Offset: 16, Range: null);
        var storage = default(DescriptorBindingPayload.WriteInfoStorage);

        _ = payload.ToWrite(0, ref storage);

        await Assert.That(storage.BufferInfo.Offset).IsEqualTo(16ul);
        await Assert.That(storage.BufferInfo.Range).IsEqualTo(Vk.WholeSize);
    }
}
