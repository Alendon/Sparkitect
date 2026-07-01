using JetBrains.Annotations;
using QuikGraph;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;

namespace Sparkitect.Graphing.Compile;

/// <summary>The Link phase of the compile: resolves each resource's symbolic epochs, validates the structural diagnostics, derives data-flow ordering, and emits a GPU-free <see cref="CompiledPlan"/>. Diagnostics are returned as <see cref="CompileError"/> cases, never thrown.</summary>
[PublicAPI]
public sealed class GraphCompiler
{
    private readonly DeclarationLedger _ledger;

    /// <summary>Creates a compiler over an already-collected ledger.</summary>
    public GraphCompiler(DeclarationLedger ledger) => _ledger = ledger;

    /// <summary>Runs Link: validates fork, unproducible-read, and cycle in turn, then derives ordering and resolves epochs into a plan.</summary>
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

        // Bind moments before building the ordering graph: the binding is what lets a cross-pass moment
        // read contribute a produce-before-consume edge (and participate in the cycle check).
        if (BindMoments(out var resolvedMoments) is { } momentError)
        {
            return momentError;
        }

        var graph = BuildOrderingGraph(resolvedMoments);

        // TopologicalSortAlgorithm throws NonAcyclicGraphException on a cycle; translate it into a
        // provenance-carrying DU case rather than letting it escape.
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

    private CompileError? BindMoments(out Dictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        resolvedMoments = [];

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

    private BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> BuildOrderingGraph(
        IReadOnlyDictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        var graph = new BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>>(allowParallelEdges: false);

        foreach (var node in _ledger.Nodes)
        {
            graph.AddVertex(node.Id);
        }

        foreach (var increment in _ledger.Increments)
        {
            AddEdge(graph, increment.SourceNode, increment.ProducedNode);
        }

        foreach (var read in _ledger.Reads)
        {
            EnsureVertex(graph, read.Reader);
            AddEdge(graph, read.EpochNode, read.Reader);
        }

        // Anti-dependency: the produced node also orders after every reader declared against its source
        // epoch, so an increment cannot clobber a reader still consuming it.
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

        // A moment with no resolved producer was already rejected by BindMoments, so an unresolved read
        // here is skipped defensively.
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

    private List<GraphNodeId> DeriveOrdering(BidirectionalGraph<GraphNodeId, Edge<GraphNodeId>> graph)
    {
        // Mint order (the ledger's node order) is the stable tiebreak among ready nodes, so the output
        // is deterministic regardless of edge-insertion order.
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
        // Repeatedly strip zero-in-degree vertices; whatever cannot be stripped participates in a cycle.
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
