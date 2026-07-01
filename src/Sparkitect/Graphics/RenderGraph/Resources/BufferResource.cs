using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public class BufferResource
{
    public VkBuffer Backing { get; }

    /// <summary>The logical byte size requested at resolve; may be below the backing's grown capacity. Mutable so a reused backing reports the latest data-driven size.</summary>
    public ulong ByteSize { get; set; }

    /// <summary>The host-mapped CPU pointer for a mapped backing; zero for a device-local backing.</summary>
    public nint MappedData => Backing.MappedData;

    /// <summary>The backing's current access mask. Mutable: callers issuing their own barriers write it directly to stay coherent.</summary>
    public AccessFlags CurrentAccess { get; set; }

    /// <summary>The backing's current pipeline stage, written alongside <see cref="CurrentAccess"/>.</summary>
    public PipelineStageFlags CurrentStage { get; set; }

    /// <summary>Constructs a leaf over one backing with its initial tracked state.</summary>
    public BufferResource(
        VkBuffer backing,
        ulong byteSize,
        AccessFlags initialAccess = 0,
        PipelineStageFlags initialStage = PipelineStageFlags.TopOfPipeBit)
    {
        Backing = backing;
        ByteSize = byteSize;
        CurrentAccess = initialAccess;
        CurrentStage = initialStage;
    }

    /// <summary>
    /// Emits a whole-buffer barrier and writes the resulting access/stage back into the leaf's carried state.
    /// No-op when both access and stage already match the requested destination.
    /// </summary>
    public void BarrierTo(
        VkCommandBuffer commandBuffer,
        PipelineStageFlags dstStage,
        AccessFlags dstAccess)
    {
        if (CurrentStage == dstStage && CurrentAccess == dstAccess) return;

        var srcStage = CurrentStage == 0 ? PipelineStageFlags.TopOfPipeBit : CurrentStage;

        commandBuffer.BufferBarrier(Backing,
            srcStage: srcStage,
            dstStage: dstStage,
            srcAccess: CurrentAccess,
            dstAccess: dstAccess);

        CurrentStage = dstStage;
        CurrentAccess = dstAccess;
    }
}
