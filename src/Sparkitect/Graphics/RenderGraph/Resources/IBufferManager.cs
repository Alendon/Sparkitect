using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph.Resources;


/// <summary>Graph-local provider of the host-mapped and device-local storage-buffer leaves, grown-to-fit and reused for the graph's lifetime.</summary>
[PublicAPI]
public interface IBufferManager
{
    /// <summary>
    /// Resolves the host-mapped storage buffer leaf. Parameterless and lazy: the first call constructs the leaf
    /// over a floor-sized backing (never zero) so the fact can build it before any data size is known; the same
    /// leaf instance is reused for the graph's lifetime and carries barrier state across uses. Size is driven at
    /// write time through <see cref="GrowHostLeaf"/>.
    /// </summary>
    BufferResource ResolveHostLeaf();

    /// <summary>
    /// Resolves the device-local storage buffer leaf. Parameterless and lazy like <see cref="ResolveHostLeaf"/>;
    /// the same physical backing spans the staging-write and shader-read uses, grown at write time through
    /// <see cref="GrowDeviceLeaf"/>.
    /// </summary>
    BufferResource ResolveDeviceLeaf();

    /// <summary>
    /// Grows the host leaf to the written byte count (floored to a nonzero minimum) identity-preservingly: the
    /// same <see cref="BufferResource"/> object swaps its backing internally so later resolves see the grown
    /// backing. A within-capacity request reuses the backing.
    /// </summary>
    BufferResource GrowHostLeaf(ulong byteSize);

    /// <summary>
    /// Grows the device leaf to the written byte count (floored to a nonzero minimum) identity-preservingly,
    /// mirroring <see cref="GrowHostLeaf"/>.
    /// </summary>
    BufferResource GrowDeviceLeaf(ulong byteSize);

    /// <summary>
    /// Frees the manager-owned buffer backings at graph teardown (the
    /// <see cref="Sparkitect.Graphing.Descriptions.CleanupStrategy.Release"/> path for the VMA backings).
    /// </summary>
    void DisposeBuffers();
}
