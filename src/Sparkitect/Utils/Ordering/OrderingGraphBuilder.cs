using JetBrains.Annotations;
using Sparkitect.Utils.DU;

namespace Sparkitect.Utils.Ordering;

/// <summary>
/// Builds a dependency graph over <typeparamref name="TNode"/> keys and produces a deterministic
/// topological order via Kahn's algorithm. This is the single shared ordering core: every consumer
/// translates its own metadata into nodes and edges, then delegates sorting here. The core never
/// throws for ordering failures — cycles and missing required dependencies surface as
/// <see cref="OrderingError{TNode}"/> arms of the returned <see cref="Result{TOk,TError}"/>.
/// </summary>
/// <typeparam name="TNode">The node key type; identity uses <see cref="EqualityComparer{T}.Default"/>.</typeparam>
[PublicAPI]
public sealed class OrderingGraphBuilder<TNode>
    where TNode : notnull
{
    private static readonly IEqualityComparer<TNode> NodeComparer = EqualityComparer<TNode>.Default;

    private readonly List<TNode> _addOrder = [];
    private readonly HashSet<TNode> _nodes = new(NodeComparer);
    private readonly List<(TNode From, TNode To, bool Optional)> _edges = [];

    /// <summary>
    /// Registers a node. Duplicate adds are ignored, and the first-seen position fixes the node's
    /// add-order (the tiebreak source for <see cref="OrderingTiebreak{TNode}.InsertionOrder"/> and the
    /// stable iteration order for cycle-participant listing).
    /// </summary>
    /// <param name="node">The node key to register.</param>
    public void AddNode(TNode node)
    {
        if (_nodes.Add(node))
        {
            _addOrder.Add(node);
        }
    }

    /// <summary>
    /// Records a directed ordering edge: <paramref name="from"/> is placed before <paramref name="to"/>.
    /// Self-edges (<paramref name="from"/> equal to <paramref name="to"/>) are no-ops, and parallel
    /// duplicate edges are deduplicated at sort time so in-degree stays correct.
    /// </summary>
    /// <param name="from">The node that must come first.</param>
    /// <param name="to">The node that must come after.</param>
    /// <param name="optional">
    /// When true, an edge whose endpoint is not a known node is silently dropped; when false, such an
    /// edge yields <see cref="OrderingError{TNode}.MissingRequiredDependency"/>.
    /// </param>
    public void AddEdge(TNode from, TNode to, bool optional)
    {
        if (NodeComparer.Equals(from, to))
        {
            return;
        }

        _edges.Add((from, to, optional));
    }

    /// <summary>
    /// Produces a deterministic topological order of the registered nodes, breaking ties by the given
    /// <paramref name="tiebreak"/>. Never throws.
    /// </summary>
    /// <param name="tiebreak">The strategy that orders ready (zero-remaining-dependency) nodes.</param>
    /// <returns>
    /// The ordered nodes on success, or an <see cref="OrderingError{TNode}"/> naming the cycle
    /// participants or the missing required dependency on failure.
    /// </returns>
    public Result<IReadOnlyList<TNode>, OrderingError<TNode>> Sort(OrderingTiebreak<TNode> tiebreak)
    {
        ArgumentNullException.ThrowIfNull(tiebreak);

        var adjacency = new Dictionary<TNode, List<TNode>>(NodeComparer);
        foreach (var node in _addOrder)
        {
            adjacency[node] = [];
        }

        var seenEdges = new HashSet<(TNode From, TNode To)>();
        foreach (var edge in _edges)
        {
            if (!_nodes.Contains(edge.From) || !_nodes.Contains(edge.To))
            {
                if (edge.Optional)
                {
                    continue;
                }

                return new OrderingError<TNode>.MissingRequiredDependency(edge.From, edge.To);
            }

            if (seenEdges.Add((edge.From, edge.To)))
            {
                adjacency[edge.From].Add(edge.To);
            }
        }

        var ordered = tiebreak.Comparer is null
            ? DrainInsertionOrder(adjacency)
            : DrainLexicographic(adjacency, tiebreak.Comparer);

        if (ordered.Count != _addOrder.Count)
        {
            return new OrderingError<TNode>.Cycle(FindCycleParticipants(adjacency));
        }

        return new Result<IReadOnlyList<TNode>, OrderingError<TNode>>.Ok(ordered);
    }

    private Dictionary<TNode, int> ComputeInDegree(Dictionary<TNode, List<TNode>> adjacency)
    {
        var inDegree = new Dictionary<TNode, int>(NodeComparer);
        foreach (var node in _addOrder)
        {
            inDegree[node] = 0;
        }

        foreach (var targets in adjacency.Values)
        {
            foreach (var target in targets)
            {
                inDegree[target]++;
            }
        }

        return inDegree;
    }

    // Stable add-order pass (ported from GraphCompiler.DeriveOrdering): among ready nodes, emit in the
    // order they were added, so output is deterministic regardless of edge-insertion order.
    private List<TNode> DrainInsertionOrder(Dictionary<TNode, List<TNode>> adjacency)
    {
        var inDegree = ComputeInDegree(adjacency);
        var ordered = new List<TNode>(_addOrder.Count);
        var emitted = new HashSet<TNode>(NodeComparer);

        while (ordered.Count < _addOrder.Count)
        {
            var progressed = false;
            foreach (var vertex in _addOrder)
            {
                if (emitted.Contains(vertex) || inDegree[vertex] != 0)
                {
                    continue;
                }

                ordered.Add(vertex);
                emitted.Add(vertex);
                progressed = true;
                foreach (var target in adjacency[vertex])
                {
                    inDegree[target]--;
                }
            }

            if (!progressed)
            {
                break;
            }
        }

        return ordered;
    }

    // Ordinal-style priority drain (ported from EntrypointOrderingResolver): ready nodes are ranked by
    // the injected comparer, smallest first.
    private List<TNode> DrainLexicographic(Dictionary<TNode, List<TNode>> adjacency, IComparer<TNode> comparer)
    {
        var inDegree = ComputeInDegree(adjacency);
        var queue = new PriorityQueue<TNode, TNode>(comparer);
        foreach (var node in _addOrder)
        {
            if (inDegree[node] == 0)
            {
                queue.Enqueue(node, node);
            }
        }

        var ordered = new List<TNode>(_addOrder.Count);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(current);
            foreach (var target in adjacency[current])
            {
                inDegree[target]--;
                if (inDegree[target] == 0)
                {
                    queue.Enqueue(target, target);
                }
            }
        }

        return ordered;
    }

    // Repeatedly strip zero-in-degree vertices (ported from GraphCompiler.FindCycleParticipants);
    // survivors with positive in-degree participate in a cycle. Iterate add-order for stable listing.
    private List<TNode> FindCycleParticipants(Dictionary<TNode, List<TNode>> adjacency)
    {
        var inDegree = ComputeInDegree(adjacency);

        bool progressed;
        do
        {
            progressed = false;
            foreach (var vertex in _addOrder)
            {
                if (inDegree[vertex] != 0)
                {
                    continue;
                }

                inDegree[vertex] = -1;
                progressed = true;
                foreach (var target in adjacency[vertex])
                {
                    inDegree[target]--;
                }
            }
        }
        while (progressed);

        var participants = new List<TNode>();
        foreach (var node in _addOrder)
        {
            if (inDegree[node] > 0)
            {
                participants.Add(node);
            }
        }

        return participants;
    }
}
