using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Compile;

/// <summary>
/// The structural outcome of a successful Link: the ledger nodes in their resolved data-flow order,
/// plus each resource chain's symbolic epochs resolved to concrete positions. This is plan structure
/// only — there is no GPU, barrier, layout, or synchronization here; emission of those is an L2
/// concern outside this core. The order is derived purely from Read/Increment edges, so two
/// declaration orders over the same declarations produce an identical plan.
/// </summary>
[PublicAPI]
public sealed class CompiledPlan
{
    internal CompiledPlan(
        IReadOnlyList<GraphNodeId> orderedNodes,
        IReadOnlyDictionary<GraphNodeId, IReadOnlyList<ResolvedEpoch>> resolvedChains,
        IReadOnlyDictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        OrderedNodes = orderedNodes;
        ResolvedChains = resolvedChains;
        ResolvedMoments = resolvedMoments;
    }

    /// <summary>The ledger nodes in resolved topological order (data-flow order over the relations).</summary>
    public IReadOnlyList<GraphNodeId> OrderedNodes { get; }

    /// <summary>
    /// Per resource chain (keyed by the declaring resource node), the chain's epochs resolved from
    /// symbolic positions to concrete ordinals — the Link-phase resolution result.
    /// </summary>
    public IReadOnlyDictionary<GraphNodeId, IReadOnlyList<ResolvedEpoch>> ResolvedChains { get; }

    /// <summary>
    /// Each referenced moment bound to the single marked increment that publishes it — the Link-phase
    /// moment binding. A consume reference resolves to its moment's result epoch through this map.
    /// </summary>
    public IReadOnlyDictionary<Identification, ResolvedMoment> ResolvedMoments { get; }
}

/// <summary>
/// A referenced moment bound to the single marked increment publishing it: the increment's produced
/// node and the result epoch a consume reference resolves to.
/// </summary>
[PublicAPI]
public readonly record struct ResolvedMoment(GraphNodeId IncrementNode, Epoch ResultEpoch);

/// <summary>
/// One symbolic epoch resolved to a concrete position within its chain. Resolution happens only in
/// Link, never at declaration time, so the concrete position is independent of pass-setup order.
/// </summary>
[PublicAPI]
public readonly record struct ResolvedEpoch(GraphNodeId Node, Epoch Epoch, int Position);
