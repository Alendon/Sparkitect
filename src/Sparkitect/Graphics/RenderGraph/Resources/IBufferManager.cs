using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;


[PublicAPI]
public interface IBufferManager
{
    /// <summary>
    /// The data-driven byte size the next leaf resolve uses. The producing side (staging) writes it from the
    /// pushed snapshot before resolve; it is a manager-owned current-size lookup, never a description parameter.
    /// </summary>
    ulong CurrentByteSize { get; set; }

    /// <summary>
    /// Resolves the host-mapped storage buffer leaf sized to <paramref name="byteSize"/>. The backing is grown
    /// to fit and reused for the graph's lifetime; the same leaf instance carries barrier state across uses.
    /// </summary>
    BufferResource ResolveHostLeaf(ulong byteSize);

    /// <summary>
    /// Resolves the device-local storage buffer leaf sized to <paramref name="byteSize"/>. Grown to fit and
    /// reused like the host leaf; the same physical backing spans the staging-write and shader-read uses.
    /// </summary>
    BufferResource ResolveDeviceLeaf(ulong byteSize);

    /// <summary>
    /// Frees the manager-owned buffer backings at graph teardown (the
    /// <see cref="Sparkitect.Graphing.Descriptions.CleanupStrategy.Release"/> path for the VMA backings).
    /// </summary>
    void DisposeBuffers();
}
