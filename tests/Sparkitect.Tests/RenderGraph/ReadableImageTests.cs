using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;

namespace Sparkitect.Tests.RenderGraph;

public class ReadableImageTests
{
    [Test]
    public async Task ReadableImage_Map_TransferSrc_ReturnsTransferSrcOptimalTuple()
    {
        var (layout, access, stage) = ReadableImage.Map(ReadUsage.TransferSrc);
        await Assert.That(layout).IsEqualTo(ImageLayout.TransferSrcOptimal);
        await Assert.That(access).IsEqualTo(AccessFlags.TransferReadBit);
        await Assert.That(stage).IsEqualTo(PipelineStageFlags.TransferBit);
    }
}
