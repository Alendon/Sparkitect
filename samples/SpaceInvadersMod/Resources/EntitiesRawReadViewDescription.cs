using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Read-usage description over the externally-pushed <c>entities_raw</c> moment. References the
/// <paramref name="Moment"/> — the Read-after-Increment ordering edge that sequences the consuming staging
/// pass after the graph-synthesized pushed birth increment — and instantiates the fact that resolves the
/// frame's bound snapshot.
/// </summary>
[PublicAPI]
public sealed record EntitiesRawReadViewDescription(Identification Moment)
    : IResourceDescription<EntitiesRawReadView>
{
    /// <inheritdoc/>
    public DeclaredFact<EntitiesRawReadView> Declare(IResourceTransaction tx)
    {
        // Reference (never increment) the pushed moment: the ordering edge after its synthesized birth increment.
        tx.ReferenceMoment(Moment);

        var fact = tx.InstantiateFact<EntitiesRawReadViewFact>();
        return fact with { Moment = Moment };
    }
}
