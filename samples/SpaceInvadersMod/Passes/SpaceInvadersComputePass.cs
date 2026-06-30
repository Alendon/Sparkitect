using System.Collections.Immutable;
using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using Sparkitect.Utils.DU;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph_Deprecated.RenderPassDeprecatedRegistry;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// Compute draw pass. Declares a storage view over the shared target image at binding 0 and a storage
/// buffer view over the shared entity buffer at binding 1, builds its pipeline from the descriptor's
/// derived set layout plus the <see cref="SpaceInvadersGameData"/> push range, and dispatches one
/// workgroup per 8x8 pixel tile. A compute-storage write view drives the General-layout transition
/// before the dispatch. Ordered after the staging pass so the device buffer holds a complete write.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_compute")]
[PassConfiguration]
[OrderAfter<SpaceInvadersStagingPass>]
internal sealed partial class SpaceInvadersComputePass(
    ISpaceInvadersRuntimeService si,
    IEntityListResourceManager entityListManager,
    IVulkanContext vulkanContext,
    IShaderManager shaderManager)
    : ComputePass, IDisposable
{
    [GraphResource] private IGraphResource<WriteableImage> _write = null!;
    [GraphResource] private IGraphResource<StorageImageView> _target = null!;
    [GraphResource] private IGraphResource<StorageBufferView> _entities = null!;
    [GraphResource] private IGraphResource<Descriptor> _descriptor = null!;

    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;

    public override void Setup(ISetupContext ctx)
    {
        _write = ctx.Declare(
            new WriteableImageRequest.FromRegistered(GraphImageID.SpaceInvadersMod.Target, WriteUsage.ComputeStorage));

        _target = ctx.Declare(new StorageImageViewRequest.FromRegistered(GraphImageID.SpaceInvadersMod.Target));
        _entities = ctx.Declare(new BufferRequest.FromRegistered(GraphBufferID.SpaceInvadersMod.Entities));

        _descriptor = ctx.Declare(new DescriptorRequest(
        [
            new DescriptorBinding(Binding: 0, ArrayIndex: 0, _target),
            new DescriptorBinding(Binding: 1, ArrayIndex: 0, _entities),
        ]));

        BuildPipeline(_descriptor.Fetch().SetLayout);
    }

    private unsafe void BuildPipeline(VkDescriptorSetLayout setLayout)
    {
        if (!shaderManager.TryGetRegisteredShaderModule(ShaderModuleID.SpaceInvadersMod.SpaceInvaders, out var shaderModule))
            throw new InvalidOperationException("Space Invaders shader not registered");

        var layoutResult = vulkanContext.CreatePipelineLayout(
            new VkPipelineLayoutCreateOptions(
                SetLayouts: [setLayout],
                PushConstantRanges:
                [
                    new PushConstantRange
                    {
                        StageFlags = ShaderStageFlags.ComputeBit,
                        Offset = 0,
                        Size = (uint)sizeof(SpaceInvadersGameData),
                    },
                ]));
        if (layoutResult is not Result<VkPipelineLayout, VkApiResult>.Ok(var pipelineLayout))
            throw new InvalidOperationException("Failed to create Space Invaders compute pipeline layout");
        _pipelineLayout = pipelineLayout;

        var pipelineResult = vulkanContext.CreateComputePipeline(
            new VkComputePipelineCreateOptions(shaderModule, _pipelineLayout));
        if (pipelineResult is not Result<VkPipeline, VkApiResult>.Ok(var pipeline))
            throw new InvalidOperationException("Failed to create Space Invaders compute pipeline");
        _computePipeline = pipeline;
    }

    public override void Execute(VkCommandBuffer commandBuffer)
    {
        var extent = si.Window.Swapchain.Extent;

        commandBuffer.BindPipeline(PipelineBindPoint.Compute, _computePipeline!);
        _descriptor.Fetch().Push(commandBuffer, _pipelineLayout!, PipelineBindPoint.Compute);

        var gameData = new SpaceInvadersGameData
        {
            EntityCount = (uint)(entityListManager.Current?.Count ?? 0),
            ScreenWidth = extent.Width,
            ScreenHeight = extent.Height,
            Padding = 0f,
            BackgroundColor = new Vector3(0.05f, 0.05f, 0.1f),
        };
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
