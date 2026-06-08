using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Tests.RenderGraph;

public class StorageBufferViewTests
{
    private static VkBuffer FakeBuffer() =>
        (VkBuffer)RuntimeHelpers.GetUninitializedObject(typeof(VkBuffer));

    [Test]
    public async Task DescriptorType_IsStorageBuffer()
    {
        var view = new StorageBufferView(FakeBuffer());

        await Assert.That(view.DescriptorType).IsEqualTo(DescriptorType.StorageBuffer);
    }

    [Test]
    public async Task DescribeBinding_ReturnsStorageBufferPayload()
    {
        var buffer = FakeBuffer();
        var view = new StorageBufferView(buffer);

        var payload = view.DescribeBinding();

        await Assert.That(payload).IsTypeOf<DescriptorBindingPayload.StorageBuffer>();
        var storageBuffer = (DescriptorBindingPayload.StorageBuffer)payload;
        await Assert.That(storageBuffer.Buffer).IsSameReferenceAs(buffer);
        await Assert.That(storageBuffer.Offset).IsEqualTo(0ul);
        await Assert.That(storageBuffer.Range).IsNull();
    }

    [Test]
    public async Task Buffer_ExposesBacking()
    {
        var buffer = FakeBuffer();
        var view = new StorageBufferView(buffer);

        await Assert.That(view.Buffer).IsSameReferenceAs(buffer);
    }
}
