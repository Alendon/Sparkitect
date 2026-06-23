using System.Collections.Immutable;
using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils.DU;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph_Deprecated.RenderPassRegistry;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace PongMod.Passes;

/// <summary>
/// Compute draw pass. Declares a storage view + push descriptor over the shared registered image,
/// builds its own pipeline/layout from the descriptor's derived set layout plus the
/// <see cref="PongGameData"/> push range, and dispatches one workgroup per 8x8 pixel tile. A
/// compute-storage write view drives the General-layout transition before the dispatch.
/// </summary>
[RenderPassRegistry.RegisterPass("pong_compute")]
internal sealed partial class PongComputePass(IPongRuntimeService pong, IVulkanContext vulkanContext, IShaderManager shaderManager)
    : ComputePass, IDisposable
{
    [GraphResource] private IGraphResource<WriteableImage> _write = null!;
    [GraphResource] private IGraphResource<StorageImageView> _target = null!;
    [GraphResource] private IGraphResource<Descriptor> _descriptor = null!;

    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;

    public override void Setup(ISetupContext ctx)
    {
        // Compute-storage write view: drives the General transition for the dispatch (D-11).
        _write = ctx.Declare(
            new WriteableImageRequest.FromRegistered(GraphImageID.PongMod.Target, WriteUsage.ComputeStorage));

        // Storage view + push descriptor binding it at (set 0, binding 0). The view must be
        // declared before the descriptor so the descriptor can read its DescriptorType at Declare.
        _target = ctx.Declare(new StorageImageViewRequest.FromRegistered(GraphImageID.PongMod.Target));
        _descriptor = ctx.Declare(new DescriptorRequest(
        [
            new DescriptorBinding(Binding: 0, ArrayIndex: 0, _target),
        ]));

        BuildPipeline(_descriptor.Fetch().SetLayout);
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

    protected override void InvokeSlotPreExecuteHooks(VkCommandBuffer commandBuffer)
    {
        // Transition the shared image to General before the compute write.
        _write.Fetch().PreExecute(commandBuffer);
    }

    public void Dispose()
    {
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
    }
}
