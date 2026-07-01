using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the copy pass's read view: it references the <c>target</c> moment (the cross-pass
/// identity the compute write view published) so the copy pass orders after the compute pass with no
/// explicit ordering attribute, then instantiates the <see cref="ReadViewFact"/>, which re-resolves the
/// same shared N=1 leaf through that moment as a blit source.
/// </summary>
[PublicAPI]
public sealed record ReadViewDescription : IResourceDescription<TransferSrcReadView>
{
    /// <inheritdoc/>
    public DeclaredFact<TransferSrcReadView> Declare(IResourceTransaction tx)
    {
        // Reference the target moment (never increment it): the Read-after-Increment edge that sequences
        // the copy pass after the compute pass. Ordering-only.
        tx.ReferenceMoment(GraphMomentID.PongMod.Target);

        return tx.InstantiateFact<ReadViewFact>();
    }
}
