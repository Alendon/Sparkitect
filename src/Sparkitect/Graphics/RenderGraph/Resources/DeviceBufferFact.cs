using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Resolves the device-local storage-buffer leaf through the graph-local buffer manager. Carries no size: the
/// byte count is data-driven at write time, so the leaf resolves parameterless (floor-sized until the first
/// write grows it). Cleanup is <see cref="CleanupStrategy.Release"/> — the manager owns the VMA backing.
/// </summary>
[FactRegistry.Register("device_buffer")]
public sealed partial record DeviceBufferFact(IBufferManager? Provider)
    : DeclaredFact<BufferResource>, IHasIdentification
{
    /// <inheritdoc/>
    public BufferResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "DeviceBufferFact.CreateInstance: no buffer backing provider was injected. The graph-local " +
                "IBufferManager must be resolvable when the fact factory builds this fact.");

        return Provider.ResolveDeviceLeaf();
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
