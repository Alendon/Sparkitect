using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding.IDs;
using RenderPassRegistry = Sparkitect.Graphics.RenderGraph_Deprecated.RenderPassRegistry;

namespace SpaceInvadersMod.Passes;

/// <summary>
/// First pass in the graph. Copies the current published entity list from the manager's mapped host
/// buffer into the shared device-local entity buffer, then barriers the device buffer so the compute
/// pass reads a complete write (TransferWrite -> ShaderRead, Transfer -> ComputeShader). Runs first,
/// so it carries no ordering edge. Reads its inputs through the entity-list manager's public
/// cross-assembly accessors and the storage-buffer view's public buffer accessor.
/// </summary>
[RenderPassRegistry.RegisterPass("space_invaders_staging")]
[PassConfiguration]
internal sealed partial class SpaceInvadersStagingPass(IEntityListResourceManager entityListManager) : ComputePass
{
    [GraphResource] private IGraphResource<StorageBufferView> _deviceBuffer = null!;

    public override void Setup(ISetupContext ctx)
    {
        _deviceBuffer = ctx.Declare(new BufferRequest.FromRegistered(GraphBufferID.SpaceInvadersMod.Entities));
    }

    public override unsafe void Execute(VkCommandBuffer commandBuffer)
    {
        var current = entityListManager.Current;
        if (current is null || current.Count == 0)
            return;

        var hostBuffer = entityListManager.HostBuffer;
        var deviceBuffer = _deviceBuffer.Fetch().Buffer;

        var byteCount = current.ByteSize;
        var source = MemoryMarshal.AsBytes(current.Elements);
        var destination = new Span<byte>((void*)hostBuffer.MappedData, (int)byteCount);
        source.CopyTo(destination);

        commandBuffer.CopyBuffer(hostBuffer, deviceBuffer, byteCount);
        commandBuffer.BufferBarrier(
            deviceBuffer,
            srcStage: PipelineStageFlags.TransferBit,
            dstStage: PipelineStageFlags.ComputeShaderBit,
            srcAccess: AccessFlags.TransferWriteBit,
            dstAccess: AccessFlags.ShaderReadBit);
    }
}
