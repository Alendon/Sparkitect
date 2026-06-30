using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph_Deprecated.RenderPassDeprecatedRegistry;

namespace PongMod.Passes;

/// <summary>
/// Pong-owned copy pass. Reads the shared render target (transfer-src) and blits it onto the
/// swapchain image (transfer-dst). Declared <c>[OrderAfter&lt;PongComputePass&gt;]</c> so the
/// compute write completes before this read; the graph owns the present transition.
/// </summary>
[RenderPassRegistry.RegisterPass("pong_copy")]
[PassConfiguration]
[OrderAfter<PongComputePass>]
internal sealed partial class PongCopyPass : ComputePass
{
    [GraphResource] private IGraphResource<ReadableImage> _source = null!;
    [GraphResource] private IGraphResource<WriteableImage> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Declare(
            new ReadableImageRequest.FromRegistered(GraphImageID.PongMod.Target, ReadUsage.TransferSrc));
        _swapchain = ctx.Declare(new WriteableImageRequest.FromSwapchain(WriteUsage.TransferDst));
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        commandBuffer.BlitFullExtent(
            _source.Fetch().VkImage, ImageLayout.TransferSrcOptimal,
            _swapchain.Fetch().VkImage, ImageLayout.TransferDstOptimal,
            Filter.Nearest);
    }

    protected override void InvokeSlotPreExecuteHooks(VkCommandBuffer commandBuffer)
    {
        // read → TransferSrcOptimal, swapchain → TransferDstOptimal
        _source.Fetch().PreExecute(commandBuffer);
        _swapchain.Fetch().PreExecute(commandBuffer);
    }
}
