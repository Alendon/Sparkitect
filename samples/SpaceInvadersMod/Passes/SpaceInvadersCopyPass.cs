using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// Copy pass: blits the shared compute target (transfer-src) onto the engine swapchain image (transfer-dst).
/// Reads the <c>target</c> moment, so it orders after the compute pass via the read edge with no explicit
/// ordering attribute; the read/write views contribute the transfer-src, transfer-dst, and present
/// transitions as lifecycle hooks, and the swapchain write view is the engine finishline publisher.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_copy")]
internal sealed partial class SpaceInvadersCopyPass : ComputePass, IHasIdentification
{
    private IGraphResource<TransferSrcReadView> _source = null!;
    private IGraphResource<SwapchainWriteView> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Use(new TransferSrcReadViewDescription { TargetMoment = GraphMomentID.SpaceInvadersMod.Target });
        _swapchain = ctx.Use(new SwapchainWriteViewDescription());
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        commandBuffer.BlitFullExtent(
            _source.Fetch().Backing, ImageLayout.TransferSrcOptimal,
            _swapchain.Fetch().Backing, ImageLayout.TransferDstOptimal,
            Filter.Nearest);
    }
}
