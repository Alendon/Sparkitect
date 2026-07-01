using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declares a read view over a caller-supplied target moment, ordering it after the producing pass via the Read-after-Increment edge with no explicit ordering attribute.</summary>
[PublicAPI]
public sealed record TransferSrcReadViewDescription : IResourceDescription<TransferSrcReadView>
{
    /// <summary>The target moment to reference; the same identity a write view published.</summary>
    public required Identification TargetMoment { get; init; }

    /// <inheritdoc/>
    public DeclaredFact<TransferSrcReadView> Declare(IResourceTransaction tx)
    {
        // Reference (never increment) the target moment: the Read-after-Increment edge that sequences this pass after the producer.
        tx.ReferenceMoment(TargetMoment);

        var fact = tx.InstantiateFact<TransferSrcReadViewFact>();
        return fact with { TargetMoment = TargetMoment };
    }
}
