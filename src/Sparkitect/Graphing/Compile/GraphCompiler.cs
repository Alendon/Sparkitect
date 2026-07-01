using JetBrains.Annotations;
using QuikGraph;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Graphing.Compile;

/// <summary>
/// The Link phase of the two-phase compile. Collection (minting nodes, recording the Read/Increment
/// relations) already happened on the supplied <see cref="DeclarationLedger"/>; Link resolves each
/// resource's symbolic epochs to concrete positions, validates the three structural diagnostics with
/// full provenance, derives data-flow ordering topologically from the relations alone, and emits a
/// GPU-free <see cref="CompiledPlan"/>.
/// </summary>
/// <remarks>
/// Resolution happens here and only here — a declaration's outcome never depends on pass-setup
/// order, so two declaration orders over the same declarations produce an identical plan. Diagnostics
/// are returned as <see cref="CompileError"/> cases, never thrown.
/// </remarks>
[PublicAPI]
public sealed class GraphCompiler
{
    private readonly DeclarationLedger _ledger;

    /// <summary>Creates a compiler over an already-collected ledger.</summary>
    public GraphCompiler(DeclarationLedger ledger) => _ledger = ledger;

    /// <summary>
    /// Runs Link: validates fork, unproducible-read, and cycle in turn (each short-circuiting to its
    /// provenance-carrying diagnostic), then derives ordering and resolves epochs into a plan.
    /// </summary>
    public Result<CompiledPlan, CompileError> Link()
    {
        if (DetectFork() is { } fork)
        {
            return fork;
        }

        if (DetectUnproducibleRead() is { } unproducible)
        {
            return unproducible;
        }

        // Bind moments BEFORE building the ordering graph: a moment reference resolves to its single
        // marked increment, and that binding is precisely what lets a cross-pass moment read
        // contribute a produce-before-consume ordering edge (and participate in the cycle check).
        if (BindMoments(out var resolvedMoments) is { } momentError)
        {
            return momentError;
        }

        var graph = BuildOrderingGraph(resolvedMoments);

        // Cycle detection via the in-repo QuikGraph idiom: TopologicalSortAlgorithm throws
        // NonAcyclicGraphException on a cycle. We translate that into a provenance-carrying DU case
        // rather than letting the exception escape (the deprecated compiler threw a string). The
        // moment edges folded in above are included, so a moment-induced cycle surfaces here.
        var topo = new TopologicalSortAlgorithm<GraphNodeId, Edge<GraphNodeId>>(graph);
        try
        {
            topo.Compute();
        }
        catch (NonAcyclicGraphException)
        {
            return new CompileError.Cycle(FindCycleParticipants(graph));
        }

        var ordered = DeriveOrdering(graph);
        return BuildPlan(ordered, resolvedMoments);
    }

    /// <summary>
    /// MOMENT BINDING: binds each referenced moment to exactly one marked increment. A referenced
    /// moment with ZERO marked increments is an <see cref="CompileError.UndefinedMoment"/> (naming the
    /// moment and its readers); TWO is a <see cref="CompileError.DuplicateMoment"/> (naming both marked
    /// increment provenances). Exactly ONE resolves the moment to that increment's result epoch.
    /// Marking rides the description ctor Identification — never a pass-level verb (binding is link-stage).
    /// </summary>
    private CompileError? BindMoments(out Dictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        resolvedMoments = [];

        // Marked increments grouped by the moment they publish (a node is marked by RecordMoment).
        var markedByMoment = new Dictionary<Identification, List<LedgerNode>>();
        foreach (var node in _ledger.Nodes)
        {
            if (!node.IsMarked)
            {
                continue;
            }

            if (!markedByMoment.TryGetValue(node.MarkedMoment, out var marks))
            {
                marks = [];
                markedByMoment[node.MarkedMoment] = marks;
            }

            marks.Add(node);
        }

        // Readers grouped by the moment they reference (preserving reference order).
        var readersByMoment = new Dictionary<Identification, List<GraphNodeId>>();
        foreach (var momentRead in _ledger.MomentReads)
        {
            if (!readersByMoment.TryGetValue(momentRead.Moment, out var readers))
            {
                readers = [];
                readersByMoment[momentRead.Moment] = readers;
            }

            readers.Add(momentRead.Reader);
        }

        foreach (var (moment, readers) in readersByMoment)
        {
            var marks = markedByMoment.TryGetValue(moment, out var found) ? found : [];
            switch (marks.Count)
            {
                case 0:
                    return new CompileError.UndefinedMoment(moment, readers);
                case > 1:
                    return new CompileError.DuplicateMoment(moment, marks[0].Id, marks[1].Id);
                default:
                    var increment = marks[0];
                    resolvedMoments[moment] = new ResolvedMoment(increment.Id, increment.Epoch);
                    break;
            }
        }

        return null;
    }

    /// <summary>
    /// FORK: any source epoch from which more than one increment is declared. Concurrent writers are
    /// structurally inexpressible — names both produced epochs and their shared source.
    /// </summary>
    private CompileError.Fork? DetectFork()
    {
        var bySource = new Dictionary<GraphNodeId, GraphNodeId>();
        foreach (var increment in _ledger.Increments)
        {
            if (bySource.TryGetValue(increment.SourceNode, out var first))
            {
                return new CompileError.Fork(increment.SourceNode, first, increment.ProducedNode);
            }

            bySource[increment.SourceNode] = increment.ProducedNode;
        }

        return null;
    }

    /// <summary>
    /// UNPRODUCIBLE READ: a Read whose target epoch has no producing increment — the base epoch is
    /// the canonical case, but any never-incremented epoch qualifies. Names the reader and the
    /// unproducible epoch node.
    /// </summary>
    private CompileError.UnproducibleRead? DetectUnproducibleRead()
    {
        var producible = new HashSet<GraphNodeId>();
        foreach (var increment in _ledger.Increments)
        {
            producible.Add(increment.ProducedNode);
        }

        foreach (var read in _ledger.Reads)
        {
            if (!producible.Contains(read.EpochNode))
            {
                return new CompileError.UnproducibleRead(read.Reader, read.EpochNode);
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the data-flow ordering graph over ledger nodes per requirements §Data-Flow Ordering:
    /// every Read orders the reader after that epoch's producing increment; every Increment orders
    /// after its source epoch and after that source epoch's declared readers (the anti-dependency);
    /// every moment read orders the reader after the marked increment the moment resolves to.
    /// </summary>
    private BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> BuildOrderingGraph(
        IReadOnlyDictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        var graph = new BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>>(allowParallelEdges: false);

        foreach (var node in _ledger.Nodes)
        {
            graph.AddVertex(node.Id);
        }

        // Increment: produced node orders after its source epoch.
        foreach (var increment in _ledger.Increments)
        {
            AddEdge(graph, increment.SourceNode, increment.ProducedNode);
        }

        // Read: the reader orders after the producing increment of the read epoch.
        foreach (var read in _ledger.Reads)
        {
            EnsureVertex(graph, read.Reader);
            AddEdge(graph, read.EpochNode, read.Reader);
        }

        // Increment anti-dependency: the produced node also orders after every reader declared
        // against its source epoch (the increment must not clobber a reader still consuming it).
        foreach (var increment in _ledger.Increments)
        {
            var source = NodeById(increment.SourceNode);
            if (source is null)
            {
                continue;
            }

            foreach (var reader in source.Readers)
            {
                EnsureVertex(graph, reader);
                AddEdge(graph, reader, increment.ProducedNode);
            }
        }

        // Moment read: the reader orders after the marked increment its moment resolves to. This is
        // the produce-before-consume edge a cross-pass moment reference contributes. A moment with no
        // resolved producer was already rejected by BindMoments (UndefinedMoment), so any read that
        // reaches here has a bound increment; a read without one is skipped defensively.
        foreach (var momentRead in _ledger.MomentReads)
        {
            if (!resolvedMoments.TryGetValue(momentRead.Moment, out var resolved))
            {
                continue;
            }

            EnsureVertex(graph, momentRead.Reader);
            AddEdge(graph, resolved.IncrementNode, momentRead.Reader);
        }

        return graph;
    }

    /// <summary>
    /// Derives the ordered node sequence with a mint-order tiebreak among equally-ready nodes (Kahn's
    /// pass), so the output is deterministic regardless of edge-insertion order. The structural
    /// guarantee that the graph is acyclic is already established by the topological-sort cycle check.
    /// </summary>
    private List<GraphNodeId> DeriveOrdering(BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> graph)
    {
        // Mint order is the ledger's node order; it is the stable tiebreak among ready nodes.
        var mintOrder = new List<GraphNodeId>();
        var seen = new HashSet<GraphNodeId>();
        foreach (var node in _ledger.Nodes)
        {
            if (seen.Add(node.Id))
            {
                mintOrder.Add(node.Id);
            }
        }

        var inDegree = new Dictionary<GraphNodeId, int>();
        foreach (var vertex in mintOrder)
        {
            inDegree[vertex] = graph.ContainsVertex(vertex) ? graph.InDegree(vertex) : 0;
        }

        var ordered = new List<GraphNodeId>(mintOrder.Count);
        var emitted = new HashSet<GraphNodeId>();
        while (ordered.Count < mintOrder.Count)
        {
            var progressed = false;
            foreach (var vertex in mintOrder)
            {
                if (emitted.Contains(vertex) || inDegree[vertex] != 0)
                {
                    continue;
                }

                ordered.Add(vertex);
                emitted.Add(vertex);
                progressed = true;

                if (graph.ContainsVertex(vertex))
                {
                    foreach (var outEdge in graph.OutEdges(vertex))
                    {
                        inDegree[outEdge.Target]--;
                    }
                }
            }

            if (!progressed)
            {
                // Unreachable: the topological-sort cycle check already proved acyclicity.
                break;
            }
        }

        return ordered;
    }

    /// <summary>Resolves each chain's symbolic epochs to concrete positions and assembles the plan.</summary>
    private CompiledPlan BuildPlan(
        IReadOnlyList<GraphNodeId> ordered,
        IReadOnlyDictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        var resolvedChains = new Dictionary<GraphNodeId, IReadOnlyList<ResolvedEpoch>>();
        foreach (var (resource, chain) in _ledger.Chains)
        {
            var resolved = new List<ResolvedEpoch>(chain.Count);
            for (var position = 0; position < chain.Count; position++)
            {
                var node = chain[position];
                resolved.Add(new ResolvedEpoch(node.Id, node.Epoch, position));
            }

            resolvedChains[resource] = resolved;
        }

        return new CompiledPlan(ordered, resolvedChains, resolvedMoments);
    }

    private List<GraphNodeId> FindCycleParticipants(BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> graph)
    {
        // Repeatedly strip zero-in-degree vertices; whatever cannot be stripped participates in a
        // cycle. Mirrors the Kahn's pass but keeps the residual instead of the emitted order.
        var inDegree = new Dictionary<GraphNodeId, int>();
        foreach (var vertex in graph.Vertices)
        {
            inDegree[vertex] = graph.InDegree(vertex);
        }

        bool progressed;
        do
        {
            progressed = false;
            foreach (var vertex in graph.Vertices)
            {
                if (inDegree[vertex] != 0)
                {
                    continue;
                }

                inDegree[vertex] = -1;
                progressed = true;
                foreach (var outEdge in graph.OutEdges(vertex))
                {
                    inDegree[outEdge.Target]--;
                }
            }
        }
        while (progressed);

        var participants = new List<GraphNodeId>();
        foreach (var node in _ledger.Nodes)
        {
            if (inDegree.TryGetValue(node.Id, out var degree) && degree > 0)
            {
                participants.Add(node.Id);
            }
        }

        return participants;
    }

    private LedgerNode? NodeById(GraphNodeId id)
    {
        foreach (var node in _ledger.Nodes)
        {
            if (node.Id == id)
            {
                return node;
            }
        }

        return null;
    }

    private static void EnsureVertex(BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> graph, GraphNodeId vertex)
    {
        if (!graph.ContainsVertex(vertex))
        {
            graph.AddVertex(vertex);
        }
    }

    private static void AddEdge(
        BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> graph,
        GraphNodeId from,
        GraphNodeId to)
    {
        if (from == to)
        {
            return;
        }

        if (!graph.ContainsEdge(from, to))
        {
            graph.AddEdge(new Edge<GraphNodeId>(from, to));
        }
    }
}
