using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Read-usage description over the published entity-list composite. References the
/// <paramref name="Moment"/> (<c>entities_gpu</c>) — the Read-after-Increment ordering edge that sequences
/// the consuming pass after the staging pass that published it — and instantiates the fact that resolves
/// the same composite instance.
/// </summary>
[PublicAPI]
public sealed record EntityListReadViewDescription(Identification Moment)
    : IResourceDescription<EntityListReadView>
{
    /// <inheritdoc/>
    public DeclaredFact<EntityListReadView> Declare(IResourceTransaction tx)
    {
        // Reference (never increment) the moment: the ordering edge after the publishing staging pass.
        tx.ReferenceMoment(Moment);

        var fact = tx.InstantiateFact<EntityListReadViewFact>();
        return fact with { Moment = Moment };
    }
}
