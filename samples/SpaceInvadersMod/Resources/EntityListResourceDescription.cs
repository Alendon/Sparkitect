using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// Description for the published entity-list composite. Reads the staged device buffer (the ordering input
/// that holds the copy result), birth-increments the composite marking the <paramref name="Moment"/>
/// (<c>entities_gpu</c>) on itself, and instantiates the fact that resolves the buffer — the
/// ClearColorImage self-increment birth-mark pattern applied to a no-manager buffer composite. The count is
/// not computed here and never lives in the ledger; it materializes on the instance at the producing pass's
/// Execute.
/// </summary>
[PublicAPI]
public sealed record EntityListResourceDescription(
    ResourceRef<BufferResource> Populated,
    Identification Moment) : IResourceDescription<EntityListResource>
{
    /// <inheritdoc/>
    public DeclaredFact<EntityListResource> Declare(IResourceTransaction tx)
    {
        // Ordering input: hold the staged device buffer so the composite orders after the staging copy.
        tx.Read(Populated);

        // Birth-increment the composite on itself, marking the entities_gpu moment (no pass authors it).
        tx.Increment(tx.Self<EntityListResource>(), Moment);

        var fact = tx.InstantiateFact<EntityListResourceFact>();
        return fact with { Populated = Populated };
    }
}
