using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace MinimalSampleMod.Passes;

[RenderPassRegistry.RegisterPass("clear_color")]
internal sealed partial class ClearColorPass : ComputePass
{
    [GraphResource] private IGraphResource<WriteableImage> _target = null!;
    private uint _frameCounter;

    public override void Setup(ISetupContext ctx)
    {
        _target = ctx.Declare(new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));
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
        commandBuffer.ClearColorImage(img.VkImage, ImageLayout.TransferDstOptimal, in clearColor);
    }

    protected override void InvokeSlotPreExecuteHooks(VkCommandBuffer commandBuffer)
    {
        _target.Fetch().PreExecute(commandBuffer);
    }
}