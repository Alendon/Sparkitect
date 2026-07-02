using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace PongMod.Passes;

/// <summary>Copy pass: blits the shared render target (transfer-src) onto the swapchain image (transfer-dst). Orders after the compute pass via the target-moment read edge, with no explicit ordering attribute; the views contribute all layout and present transitions as lifecycle hooks.</summary>
[RenderPassRegistry.RegisterPass("pong_copy")]
internal sealed partial class PongCopyPass : ComputePass, IHasIdentification
{
    private IGraphResource<TransferSrcReadView> _source = null!;
    private IGraphResource<SwapchainWriteView> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Use(new TransferSrcReadViewDescription { TargetMoment = GraphMomentID.PongMod.Target });
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
