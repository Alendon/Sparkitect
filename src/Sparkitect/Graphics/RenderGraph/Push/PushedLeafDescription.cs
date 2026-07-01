using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Push;

/// <summary>
/// The chain-head resource of an externally-pushed moment: a CPU-only holder the frame-start push step
/// binds the latest snapshot onto. Epoch 0 carries no element count — the count materializes only on the
/// downstream published composite — so consumers cast the raw bytes to their own element type.
/// </summary>
[PublicAPI]
public sealed class PushedResource
{
    /// <summary>The latest bound snapshot bytes for this pushed moment; empty until the first bind.</summary>
    public ReadOnlyMemory<byte> Data { get; private set; }

    /// <summary>Binds <paramref name="data"/> as this frame's snapshot for the pushed moment.</summary>
    public void Bind(ReadOnlyMemory<byte> data) => Data = data;
}

/// <summary>
/// The synthesized chain-head producer for a pushed moment: declares the pushed resource and marks the
/// moment on its birth increment, so <c>GraphCompiler.BindMoments</c> has a marked increment to bind and
/// the moment's readers link with no pass authoring the mark. The render graph declares one per
/// registered externally-pushed moment at setup, mirroring the finishline reader declaration.
/// </summary>
[PublicAPI]
public sealed record PushedLeafDescription(Identification Moment) : IResourceDescription<PushedResource>
{
    /// <inheritdoc/>
    public DeclaredFact<PushedResource> Declare(IResourceTransaction tx)
    {
        // The chain head's birth increment, marked with the pushed moment; no count at epoch 0.
        tx.Increment(tx.Self<PushedResource>(), Moment);
        return new PushedLeafFact();
    }
}

/// <summary>The fact for a pushed leaf; builds a fresh <see cref="PushedResource"/> the frame-start step
/// binds. The graph owns the snapshot bytes, so the instance itself needs no release.</summary>
[PublicAPI]
public sealed record PushedLeafFact : DeclaredFact<PushedResource>
{
    /// <inheritdoc/>
    public PushedResource CreateInstance(IInstanceContext ctx) => new();

    /// <inheritdoc/>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
