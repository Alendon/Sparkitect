using System.Runtime.CompilerServices;
using Moq;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Tests.RenderGraph;

public class BufferResourceManagerTests
{
    private static readonly Identification PassA = Identification.Create(1, 1, 1);
    private static readonly Identification KnownBuffer = Identification.Create(1, 2, 1);
    private static readonly Identification UnknownBuffer = Identification.Create(1, 2, 9);

    private static VkBuffer FakeBuffer() =>
        (VkBuffer)RuntimeHelpers.GetUninitializedObject(typeof(VkBuffer));

    private static (BufferResourceManager Manager, Func<ulong?> CapturedSize) ManagerCapturing(
        IResourceRegistrationStore store)
    {
        ulong? captured = null;
        var ctx = new Mock<IVulkanContext>(MockBehavior.Loose);
        ctx.Setup(c => c.CreateDeviceStorageBuffer(It.IsAny<ulong>(), It.IsAny<CallerContext>()))
            .Returns((ulong size, CallerContext _) =>
            {
                captured = size;
                return new Result<VkBuffer, VkApiResult>.Ok(FakeBuffer());
            });
        return (new BufferResourceManager(ctx.Object, store), () => captured);
    }

    [Test]
    public async Task Declare_FromRegistered_AfterDrain_ResolvesView()
    {
        var store = new ResourceRegistrationStore();
        store.RegisterBuffer(KnownBuffer, new BufferDescription(16, 256));
        var (mgr, capturedSize) = ManagerCapturing(store);

        mgr.DrainRegisteredBuffers();
        IBufferResourceManager typed = mgr;

        var handle = typed.Declare(PassA, 0, new BufferRequest.FromRegistered(KnownBuffer));

        await Assert.That(handle.Slot).IsEqualTo(0);
        await Assert.That(handle.Fetch()).IsNotNull();
        await Assert.That(handle.Fetch().DescriptorType)
            .IsEqualTo(Silk.NET.Vulkan.DescriptorType.StorageBuffer);
        // 256 is already a power of two: byte size = stride(16) * capacity(256).
        await Assert.That(capturedSize()).IsEqualTo(16ul * 256ul);
    }

    [Test]
    public async Task Declare_FromRegistered_Unregistered_Throws()
    {
        var store = new ResourceRegistrationStore();
        store.RegisterBuffer(KnownBuffer, new BufferDescription(16, 256));
        var (mgr, _) = ManagerCapturing(store);

        mgr.DrainRegisteredBuffers();
        IBufferResourceManager typed = mgr;

        await Assert.That(() => typed.Declare(PassA, 0, new BufferRequest.FromRegistered(UnknownBuffer)))
            .Throws<InvalidOperationException>();
    }
}
