using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph_Deprecated.RenderPassDeprecatedRegistry;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// Copy pass. Reads the shared render target (transfer-src) and blits it onto the swapchain image
/// (transfer-dst). Ordered after the compute pass so the compute write completes before this read;
/// the graph owns the present transition.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_copy")]
[PassConfiguration]
[OrderAfter<SpaceInvadersComputePass>]
internal sealed partial class SpaceInvadersCopyPass : ComputePass
{
    [GraphResource] private IGraphResource<ReadableImage> _source = null!;
    [GraphResource] private IGraphResource<WriteableImage> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Declare(
            new ReadableImageRequest.FromRegistered(GraphImageID.SpaceInvadersMod.Target, ReadUsage.TransferSrc));
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
        // source → TransferSrcOptimal, swapchain → TransferDstOptimal
        _source.Fetch().PreExecute(commandBuffer);
        _swapchain.Fetch().PreExecute(commandBuffer);
    }
}
