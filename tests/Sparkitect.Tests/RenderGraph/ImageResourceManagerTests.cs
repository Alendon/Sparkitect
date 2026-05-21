using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Image = Sparkitect.Graphics.RenderGraph.Resources.Image;

namespace Sparkitect.Tests.RenderGraph;

public class ImageResourceManagerTests
{
    private static readonly Identification PassA = Identification.Create(1, 1, 1);

    private static Image MakeSwapchainImage(int backingCount = 1)
    {
        var backings = new VkImage[backingCount];
        for (var i = 0; i < backingCount; i++) backings[i] = default!;
        return new Image(backings, new Extent2D(800, 600), Format.B8G8R8A8Unorm, initialQueueFamily: 0);
    }

    [Test]
    public async Task Declare_PreservesCallerSuppliedSlots()
    {
        var swap = MakeSwapchainImage();
        IImageResourceManager mgr = new ImageResourceManager(swap);

        var h0 = mgr.Declare(PassA, 0, new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));
        var h1 = mgr.Declare(PassA, 1, new WriteableImageRequest.FromSwapchain(WriteUsage.ComputeStorage));

        await Assert.That(h0.Slot).IsEqualTo(0);
        await Assert.That(h1.Slot).IsEqualTo(1);
        await Assert.That(h0.Fetch().UnderlyingImage).IsSameReferenceAs(swap);
        await Assert.That(h1.Fetch().UnderlyingImage).IsSameReferenceAs(swap);
        await Assert.That(h0.Fetch().Usage).IsEqualTo(WriteUsage.TransferDst);
        await Assert.That(h1.Fetch().Usage).IsEqualTo(WriteUsage.ComputeStorage);
    }

    [Test]
    public async Task DeclareUntyped_UnknownRequestArmType_Throws()
    {
        var mgr = new ImageResourceManager(MakeSwapchainImage());
        IGraphResourceManagerFor<WriteableImage> typed = mgr;

        var bogus = new BogusWriteableRequest();
        await Assert.That(() => typed.DeclareUntyped(PassA, 0, bogus))
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task WriteableImage_Map_TransferDst_ReturnsTransferDstOptimalTuple()
    {
        var (layout, access, stage) = WriteableImage.Map(WriteUsage.TransferDst);
        await Assert.That(layout).IsEqualTo(ImageLayout.TransferDstOptimal);
        await Assert.That(access).IsEqualTo(AccessFlags.TransferWriteBit);
        await Assert.That(stage).IsEqualTo(PipelineStageFlags.TransferBit);
    }

    [Test]
    public async Task WriteableImage_Map_ComputeStorage_ReturnsGeneralShaderWriteCompute()
    {
        var (layout, access, stage) = WriteableImage.Map(WriteUsage.ComputeStorage);
        await Assert.That(layout).IsEqualTo(ImageLayout.General);
        await Assert.That(access).IsEqualTo(AccessFlags.ShaderWriteBit);
        await Assert.That(stage).IsEqualTo(PipelineStageFlags.ComputeShaderBit);
    }

    [Test]
    public async Task WriteableImage_Map_ColorAttachment_ReturnsColorAttachmentTuple()
    {
        var (layout, access, stage) = WriteableImage.Map(WriteUsage.ColorAttachment);
        await Assert.That(layout).IsEqualTo(ImageLayout.ColorAttachmentOptimal);
        await Assert.That(access).IsEqualTo(AccessFlags.ColorAttachmentWriteBit);
        await Assert.That(stage).IsEqualTo(PipelineStageFlags.ColorAttachmentOutputBit);
    }

    private sealed class BogusWriteableRequest : IResourceRequest<WriteableImage>;
}
