using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphing;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace MinimalSampleMod.Passes;

[RenderPassRegistry.RegisterPass("clear_color")]
internal sealed partial class ClearColorPass : ComputePass
{
    private IGraphResource<ImageResource> _target = null!;
    private uint _frameCounter;

    public override void Setup(ISetupContext ctx)
    {
        _target = ctx.Use(new ClearColorImageDescription());
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        var img = _target.Fetch();
        var t = _frameCounter++ % 360u / 360f;
        var clearColor = new ClearColorValue
        {
            Float32_0 = t,
            Float32_1 = 1f - t,
            Float32_2 = 0.5f,
            Float32_3 = 1f,
        };

        img.TransitionTo(
            commandBuffer,
            ImageLayout.TransferDstOptimal,
            AccessFlags.TransferWriteBit,
            PipelineStageFlags.TransferBit);
        commandBuffer.ClearColorImage(img.Backing, ImageLayout.TransferDstOptimal, in clearColor);
    }
}
