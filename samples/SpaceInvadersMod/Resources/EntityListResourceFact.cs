using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Builds the published entity-list composite by resolving the staged device buffer flowed in from the
/// description. A no-manager composite: the device buffer leaf owns its own Release via the buffer manager,
/// so cleanup is <see cref="CleanupStrategy.None"/>. The count is unset until the producing pass's Execute
/// seals it. The ref is an init-property built via <c>InstantiateFact</c> + <c>with</c> (a value-type
/// <see cref="ResourceRef{T}"/> ctor param would break the keyed factory).
/// </summary>
[FactRegistry.Register("entity_list")]
public sealed partial record EntityListResourceFact
    : DeclaredFact<EntityListResource>, IHasIdentification
{
    /// <summary>The staged device buffer ref, resolved into the composite at build.</summary>
    public ResourceRef<BufferResource> Populated { get; init; }

    /// <inheritdoc/>
    public EntityListResource CreateInstance(IInstanceContext ctx) =>
        new(ctx.Resolve(Populated));

    /// <inheritdoc/>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
