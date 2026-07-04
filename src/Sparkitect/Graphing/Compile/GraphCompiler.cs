using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;

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

        // The shared ordering core returns diagnostics rather than throwing: a cycle surfaces as an
        // OrderingError.Cycle that maps straight onto CompileError.Cycle — no exception translation, so
        // the "diagnostics returned, never thrown" contract holds end to end.
        return graph.Sort(OrderingTiebreak<GraphNodeId>.InsertionOrder) switch
        {
            Result<IReadOnlyList<GraphNodeId>, OrderingError<GraphNodeId>>.Ok ordered =>
                BuildPlan(ordered.Value, resolvedMoments),
            Result<IReadOnlyList<GraphNodeId>, OrderingError<GraphNodeId>>.Error failure =>
                MapOrderingError(failure.Value),
        };
    }

    private static CompileError MapOrderingError(OrderingError<GraphNodeId> error) => error switch
    {
        OrderingError<GraphNodeId>.Cycle cycle => new CompileError.Cycle(cycle.Participants),

        // Unreachable: every edge endpoint is a ledger node registered via AddNode, so no required edge
        // can dangle. Fail loud rather than silently mint an incomplete plan if that invariant breaks.
        OrderingError<GraphNodeId>.MissingRequiredDependency missing =>
            throw new InvalidOperationException(
                $"Ordering graph referenced an unregistered dependency {missing.From} -> {missing.To}; "
                + "GraphCompiler wires edges only between registered ledger nodes."),
    };

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

    private OrderingGraphBuilder<GraphNodeId> BuildOrderingGraph(
        IReadOnlyDictionary<Identification, ResolvedMoment> resolvedMoments)
    {
        var graph = new OrderingGraphBuilder<GraphNodeId>();

        // Register in mint order: the ledger's node order is the stable insertion-order tiebreak the
        // core drains by, so the plan order is deterministic regardless of edge-insertion order.
        foreach (var node in _ledger.Nodes)
        {
            graph.AddNode(node.Id);
        }

        // Every endpoint below is a ledger node (readers are resource ids, themselves ledger nodes), so
        // all edges are required — the core's self-edge skip and parallel dedup subsume the old helpers.
        foreach (var increment in _ledger.Increments)
        {
            graph.AddEdge(increment.SourceNode, increment.ProducedNode, optional: false);
        }

        foreach (var read in _ledger.Reads)
        {
            graph.AddEdge(read.EpochNode, read.Reader, optional: false);
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
                graph.AddEdge(reader, increment.ProducedNode, optional: false);
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

            graph.AddEdge(resolved.IncrementNode, momentRead.Reader, optional: false);
        }

        return graph;
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
}
