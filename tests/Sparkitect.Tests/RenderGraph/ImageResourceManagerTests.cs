using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Image = Sparkitect.Graphics.RenderGraph.Resources.Image;

namespace Sparkitect.Tests.RenderGraph;

public class ImageResourceManagerTests
{
    private static readonly Identification PassA = Identification.Create(1, 1, 1);

    private static SwapchainResource MakeSwapchainResource(int backingCount = 1)
    {
        var swap = (VkSwapchain)RuntimeHelpers.GetUninitializedObject(typeof(VkSwapchain));
        ImagesField(swap) = new VkImage[backingCount];
        SetImageFormat(swap, Format.B8G8R8A8Unorm);
        SetExtent(swap, new Extent2D(800, 600));
        return new SwapchainResource(swap);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_images")]
    private static extern ref VkImage[] ImagesField(VkSwapchain s);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ImageFormat")]
    private static extern void SetImageFormat(VkSwapchain s, Format value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Extent")]
    private static extern void SetExtent(VkSwapchain s, Extent2D value);

    [Test]
    public async Task Declare_PreservesCallerSuppliedSlots()
    {
        var sr = MakeSwapchainResource();
        var mgr = new ImageResourceManager();
        mgr.Apply(sr);
        IImageResourceManager imgr = mgr;

        var h0 = imgr.Declare(PassA, 0, new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));
        var h1 = imgr.Declare(PassA, 1, new WriteableImageRequest.FromSwapchain(WriteUsage.ComputeStorage));

        await Assert.That(h0.Slot).IsEqualTo(0);
        await Assert.That(h1.Slot).IsEqualTo(1);
        await Assert.That(h0.Fetch().Usage).IsEqualTo(WriteUsage.TransferDst);
        await Assert.That(h1.Fetch().Usage).IsEqualTo(WriteUsage.ComputeStorage);
        await Assert.That(h0.Fetch().UnderlyingImage).IsSameReferenceAs(h1.Fetch().UnderlyingImage);
    }

    [Test]
    public async Task DeclareUntyped_UnknownRequestArmType_Throws()
    {
        var mgr = new ImageResourceManager();
        IGraphResourceManagerFor<WriteableImage> typed = mgr;

        var bogus = new BogusWriteableRequest();
        await Assert.That(() => typed.DeclareUntyped(PassA, 0, bogus))
            .Throws<InvalidCastException>();
    }

    [Test]
    public async Task Declare_BeforeApply_FetchThrows()
    {
        var mgr = new ImageResourceManager();
        IImageResourceManager imgr = mgr;

        var handle = imgr.Declare(PassA, 0, new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));

        await Assert.That(() => handle.Fetch())
            .Throws<InvalidOperationException>()
            .WithMessageContaining("Swapchain not applied");
    }

    [Test]
    public async Task ReApply_TrackedHandles_PointAtNewSwapchain()
    {
        var mgr = new ImageResourceManager();
        IImageResourceManager imgr = mgr;

        var firstSr = MakeSwapchainResource();
        mgr.Apply(firstSr);

        var handle = imgr.Declare(PassA, 0, new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));
        var imageAfterFirstApply = handle.Fetch().UnderlyingImage;

        var secondSr = MakeSwapchainResource(backingCount: 2);
        mgr.Apply(secondSr);

        var imageAfterReApply = handle.Fetch().UnderlyingImage;

        await Assert.That(imageAfterReApply).IsNotSameReferenceAs(imageAfterFirstApply);
        await Assert.That(handle.Fetch().Usage).IsEqualTo(WriteUsage.TransferDst);
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
