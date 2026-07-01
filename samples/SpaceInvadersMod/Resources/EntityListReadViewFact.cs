using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Builds the read view by resolving the published entity-list composite through the moment — the same N=1
/// <see cref="EntityListResource"/> the staging pass published, so the compute pass reads the element count
/// straight off it. No manager and no GPU object of its own, so cleanup is <see cref="CleanupStrategy.None"/>.
/// </summary>
[FactRegistry.Register("entity_list_read")]
public sealed partial record EntityListReadViewFact
    : DeclaredFact<EntityListReadView>, IHasIdentification
{
    /// <summary>The moment resolved to the shared published composite, flowed in from the description.</summary>
    public Identification Moment { get; init; }

    /// <inheritdoc/>
    public EntityListReadView CreateInstance(IInstanceContext ctx) =>
        new(ctx.ResolveMoment<EntityListResource>(Moment));

    /// <inheritdoc/>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
