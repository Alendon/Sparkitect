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

/// <summary>Compute pass: dispatches one workgroup per 8x8 pixel tile over the shared storage-write target via a push descriptor. The write view contributes the General-layout transition as a pre-execute hook, so the pass performs no layout transition itself.</summary>
[RenderPassRegistry.RegisterPass("pong_compute")]
internal sealed partial class PongComputePass(IPongRuntimeService pong, IVulkanContext vulkanContext, IShaderManager shaderManager)
    : ComputePass
{
    private IGraphResource<StorageWriteView> _write = null!;
    private IGraphResource<PongDescriptor> _descriptor = null!;

    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;

    public override void Setup(ISetupContext ctx)
    {
        _write = ctx.Use(new WriteViewDescription());

        var descriptor = new PongDescriptorDescription(_write);
        _descriptor = ctx.Use(descriptor);

        // The set layout is produced during Declare (run inside ctx.Use's setup transaction) and is
        // frame-independent, so the pass-owned pipeline is built here at Setup rather than deferred to Fetch.
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

    public override void Dispose()
    {
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
    }
}
