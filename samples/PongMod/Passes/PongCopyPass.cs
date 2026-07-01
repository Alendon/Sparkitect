using PongMod.Resources;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;

namespace PongMod.Passes;

/// <summary>
/// Pong-owned copy pass on the new render-graph model. Reads the shared render target (transfer-src) and
/// blits it onto the swapchain image (transfer-dst). The read view references the <c>target</c> moment,
/// so the copy pass orders after the compute pass via the data-flow (Read-after-Increment) edge with no
/// explicit ordering attribute. Both layout transitions and the present transition are contributed by the
/// views as lifecycle hooks; the pass issues none itself.
/// </summary>
[RenderPassRegistry.RegisterPass("pong_copy")]
internal sealed partial class PongCopyPass : ComputePass
{
    private IGraphResource<TransferSrcReadView> _source = null!;
    private IGraphResource<SwapchainWriteView> _swapchain = null!;

    public override void Setup(ISetupContext ctx)
    {
        _source = ctx.Use(new ReadViewDescription());
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
