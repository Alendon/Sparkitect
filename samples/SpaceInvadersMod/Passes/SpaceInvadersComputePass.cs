using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using SpaceInvadersMod.Resources;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils.DU;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph.RenderPassRegistry;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// Compute draw pass. Binds the shared target's storage-write view at slot 0 and the published entity-list
/// composite (device buffer) at slot 1 through one push descriptor, builds its pipeline from the derived set
/// layout plus the <see cref="SpaceInvadersGameData"/> push range, and dispatches one workgroup per 8x8 pixel
/// tile. The entity count is read straight off the composite via the <c>entities_gpu</c> moment — never
/// through a DI-fetched manager. Data-flow ordering (reads entities_gpu, publishes target) sequences it after
/// staging and before copy; the write and read views contribute all layout/sync as pre-execute hooks.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_compute")]
internal sealed partial class SpaceInvadersComputePass(
    ISpaceInvadersRuntimeService si,
    IVulkanContext vulkanContext,
    IShaderManager shaderManager)
    : ComputePass
{
    private IGraphResource<EntityListReadView> _input = null!;
    private IGraphResource<StorageWriteView> _write = null!;
    private IGraphResource<DescriptorResource> _descriptor = null!;

    private VkPipelineLayout? _pipelineLayout;
    private VkPipeline? _computePipeline;

    public override void Setup(ISetupContext ctx)
    {
        // Read the published composite off the entities_gpu moment (count + device buffer); its pre-execute
        // hook reconciles the device buffer transfer->compute.
        _input = ctx.Use(new EntityListReadViewDescription(GraphMomentID.SpaceInvadersMod.EntitiesGpu));

        // Publish the shared target; the write view contributes the General-layout transition as a hook.
        _write = ctx.Use(new StorageWriteViewDescription { TargetMoment = GraphMomentID.SpaceInvadersMod.Target });

        // image@0 from the write view, buffer@1 from the entity-list device buffer.
        var descriptor = new DescriptorResourceDescription(
            new DescriptorBinding(DescriptorType.StorageImage, _write),
            new DescriptorBinding(DescriptorType.StorageBuffer, _input));
        _descriptor = ctx.Use(descriptor);

        BuildPipeline(descriptor.SetLayout);
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
            EntityCount = (uint)_input.Fetch().Count,
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

    public override void Dispose()
    {
        _computePipeline?.Dispose();
        _pipelineLayout?.Dispose();
    }
}
