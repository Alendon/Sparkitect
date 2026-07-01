using PongMod.CompilerGenerated.IdExtensions;
using PongMod.Resources;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils.DU;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace PongMod.Passes;

/// <summary>
/// Compute draw pass on the new render-graph model. Uses the single storage write view over the shared
/// target (which publishes the <c>target</c> moment) plus a push descriptor composed over it, builds its
/// own pipeline/layout from the descriptor's derived set layout plus the <see cref="PongGameData"/> push
/// range, and dispatches one workgroup per 8x8 pixel tile. The write view contributes the General-layout
/// transition as a lifecycle hook — the render graph dispatches it before this pass's Execute, so the
/// pass performs no layout transition itself.
/// </summary>
[RenderPassRegistry.RegisterPass("pong_compute")]
internal sealed partial class PongComputePass(IPongRuntimeService pong, IVulkanContext vulkanContext, IShaderManager shaderManager)
    : ComputePass, IDisposable
{
    private IGraphResource<StorageWriteView> _write = null!;
    private IGraphResource<PongDescriptor> _descriptor = null!;

    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;

    public override void Setup(ISetupContext ctx)
    {
        // One write view over the shared target (D-14 — the deprecated write + storage-view collapse into
        // one composite): it births/publishes the target moment and drives the General transition via its
        // pre-execute hook. The push descriptor binds that view at (set 0, binding 0) and derives its set
        // layout at Declare through the graph-local cache.
        _write = ctx.Use(new WriteViewDescription());

        var descriptor = new PongDescriptorDescription(_write);
        _descriptor = ctx.Use(descriptor);

        // The descriptor's set layout is produced during its Declare (which ctx.Use runs inside the setup
        // transaction) and is frame-independent — cache-owned (D-12), fully determined by the bound view's
        // static descriptor type (D-11). So the pass-owned pipeline/layout is built here at Setup, read
        // straight off the description. No first-frame Fetch deferral: Fetch is a runtime (per-frame
        // instance) concern, but a set layout is Setup-derived data the description exposes directly.
        BuildPipeline(descriptor.SetLayout);
    }

    private unsafe void BuildPipeline(VkDescriptorSetLayout setLayout)
    {
        if (!shaderManager.TryGetRegisteredShaderModule(ShaderModuleID.PongMod.Pong, out var shaderModule))
            throw new InvalidOperationException("Pong shader not registered");

        var layoutResult = vulkanContext.CreatePipelineLayout(
            new VkPipelineLayoutCreateOptions(
                SetLayouts: [setLayout],
                PushConstantRanges:
                [
                    new PushConstantRange
                    {
                        StageFlags = ShaderStageFlags.ComputeBit,
                        Offset = 0,
                        Size = (uint)sizeof(PongGameData),
                    },
                ]));
        if (layoutResult is not Result<VkPipelineLayout, VkApiResult>.Ok(var pipelineLayout))
            throw new InvalidOperationException("Failed to create Pong compute pipeline layout");
        _pipelineLayout = pipelineLayout;

        var pipelineResult = vulkanContext.CreateComputePipeline(
            new VkComputePipelineCreateOptions(shaderModule, _pipelineLayout));
        if (pipelineResult is not Result<VkPipeline, VkApiResult>.Ok(var pipeline))
            throw new InvalidOperationException("Failed to create Pong compute pipeline");
        _computePipeline = pipeline;
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        var extent = pong.Window.Swapchain.Extent;

        commandBuffer.BindPipeline(PipelineBindPoint.Compute, _computePipeline!);
        _descriptor.Fetch().Push(commandBuffer, _pipelineLayout!, PipelineBindPoint.Compute);

        ref var gameData = ref pong.GameData;
        gameData.ScreenWidth = extent.Width;
        gameData.ScreenHeight = extent.Height;
        gameData.BackgroundColor = pong.BackgroundColor;
        commandBuffer.PushConstants(_pipelineLayout!, ShaderStageFlags.ComputeBit, 0, in gameData);

        var groupCountX = (extent.Width + 7) / 8;
        var groupCountY = (extent.Height + 7) / 8;
        commandBuffer.Dispatch(groupCountX, groupCountY, 1);
    }

    public void Dispose()
    {
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
    }
}
