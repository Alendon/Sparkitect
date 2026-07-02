using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The immutable facts of the staging composite: holds the host, device, and post-increment (staged) refs
/// minted by <see cref="StagingDescription"/>. <see cref="CreateInstance"/> resolves the host and device
/// leaves into a <see cref="StagingBuffer"/>; the staged ref is graph-side ordering truth, exposed pass-side
/// as the description's <c>PopulatedBuffer</c> declaration product. Cleanup is
/// <see cref="CleanupStrategy.None"/> — the host and device leaves own their own Release via the buffer manager.
/// </summary>
[FactRegistry.Register("staging")]
public sealed partial record StagingFacts(IBufferManager? Provider)
    : DeclaredFact<StagingBuffer>, IHasIdentification
{
    /// <summary>The host-mapped leaf ref (base epoch); the CPU memcpy target.</summary>
    public ResourceRef<BufferResource> Host { get; init; }

    /// <summary>The device-local leaf ref (base epoch); the copy destination.</summary>
    public ResourceRef<BufferResource> Device { get; init; }

    /// <summary>The post-increment device ref (X:0-&gt;X:1); graph-side ordering truth for the staging copy.</summary>
    public ResourceRef<BufferResource> Staged { get; init; }

    /// <inheritdoc/>
    public StagingBuffer CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "StagingFacts.CreateInstance: no buffer backing provider was injected. The graph-local " +
                "IBufferManager must be resolvable when the fact factory builds this fact.");

        return new(ctx.Resolve(Host), ctx.Resolve(Device), Provider);
    }

    /// <summary>Holds no independently-owned backing; releases nothing at teardown.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
