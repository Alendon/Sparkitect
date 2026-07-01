using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>Declares the copy pass's read view over the <c>target</c> moment, ordering it after the compute pass with no explicit ordering attribute.</summary>
[PublicAPI]
public sealed record ReadViewDescription : IResourceDescription<TransferSrcReadView>
{
    /// <inheritdoc/>
    public DeclaredFact<TransferSrcReadView> Declare(IResourceTransaction tx)
    {
        // Reference (never increment) the target moment: the Read-after-Increment edge that sequences this
        // pass after the compute pass.
        tx.ReferenceMoment(GraphMomentID.PongMod.Target);

        return tx.InstantiateFact<ReadViewFact>();
    }
}
