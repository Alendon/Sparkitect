using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Push;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Builds the pushed-snapshot read view by resolving the frame's bound <see cref="PushedResource"/> through
/// the <c>entities_raw</c> moment. The graph owns the snapshot bytes, so this view holds no GPU object and
/// cleanup is <see cref="CleanupStrategy.None"/>.
/// </summary>
[FactRegistry.Register("entities_raw_read")]
public sealed partial record EntitiesRawReadViewFact
    : DeclaredFact<EntitiesRawReadView>, IHasIdentification
{
    /// <summary>The pushed moment resolved to the frame's bound snapshot, flowed in from the description.</summary>
    public Identification Moment { get; init; }

    /// <inheritdoc/>
    public EntitiesRawReadView CreateInstance(IInstanceContext ctx) =>
        new(ctx.ResolveMoment<PushedResource>(Moment));

    /// <inheritdoc/>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
