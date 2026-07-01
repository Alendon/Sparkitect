using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Resolves the host-mapped storage-buffer leaf through the graph-local buffer manager. Carries no size: the
/// byte count is data-driven, read from the manager's current-size lookup at resolve. Cleanup is
/// <see cref="CleanupStrategy.Release"/> — the manager owns the VMA backing.
/// </summary>
[FactRegistry.Register("host_buffer")]
public sealed partial record HostBufferFact(IBufferManager? Provider)
    : DeclaredFact<BufferResource>, IHasIdentification
{
    /// <inheritdoc/>
    public BufferResource CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "HostBufferFact.CreateInstance: no buffer backing provider was injected. The graph-local " +
                "IBufferManager must be resolvable when the fact factory builds this fact.");

        return Provider.ResolveHostLeaf(Provider.CurrentByteSize);
    }

    public CleanupStrategy CleanupStrategy => CleanupStrategy.Release;
}
