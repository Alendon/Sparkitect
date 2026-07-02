using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public class BufferResource
{
    /// <summary>The VMA backing. Private set so a grow can swap it in place (identity-preserving) via <see cref="SwapBacking"/>.</summary>
    public VkBuffer Backing { get; private set; }

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
    /// Swaps in a freshly grown backing while keeping this object's identity, so consumers that resolve the
    /// same leaf later in the frame observe the grown backing. The new backing is untracked hardware, so the
    /// carried barrier state resets to top-of-pipe — the next barrier transitions from a clean baseline.
    /// </summary>
    internal void SwapBacking(VkBuffer backing, ulong byteSize)
    {
        Backing = backing;
        ByteSize = byteSize;
        CurrentAccess = 0;
        CurrentStage = PipelineStageFlags.TopOfPipeBit;
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
