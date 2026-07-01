using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Compile;

/// <summary>The structural outcome of a successful Link: ordered nodes plus each chain's epochs resolved to concrete positions. No GPU, barrier, or layout concerns.</summary>
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

    /// <summary>The ledger nodes in resolved data-flow order.</summary>
    public IReadOnlyList<GraphNodeId> OrderedNodes { get; }

    /// <summary>Per resource chain (keyed by declaring node), the chain's epochs resolved from symbolic positions to concrete ordinals.</summary>
    public IReadOnlyDictionary<GraphNodeId, IReadOnlyList<ResolvedEpoch>> ResolvedChains { get; }

    /// <summary>Each referenced moment bound to the single marked increment that publishes it.</summary>
    public IReadOnlyDictionary<Identification, ResolvedMoment> ResolvedMoments { get; }
}

/// <summary>A referenced moment bound to the marked increment publishing it: the produced node and the result epoch a consume resolves to.</summary>
[PublicAPI]
public readonly record struct ResolvedMoment(GraphNodeId IncrementNode, Epoch ResultEpoch);

/// <summary>One symbolic epoch resolved to a concrete position within its chain.</summary>
[PublicAPI]
public readonly record struct ResolvedEpoch(GraphNodeId Node, Epoch Epoch, int Position);
