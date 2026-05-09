using QuikGraph;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.Modding;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Pure-logic graph builder + compiler. Owned by <c>RenderGraph</c>; extracted
/// as a separate <c>internal sealed class</c> per Phase 49 Open Question 4 lock so the
/// compilation logic is unit-testable without a Vulkan device or DI container.
/// </summary>
/// <remarks>
/// <para>
/// Per D-D3, the ordering-edge list is an internal-only seam at Phase 49 — Phase 53 adds
/// the public surface (method / attribute / interface) and routes contributions through
/// <see cref="AddOrderingEdgeInternal"/>. At walking-skeleton scope no public consumer
/// exists, so the edge list is always empty in production. Tests use
/// <see cref="AddOrderingEdgeInternal"/> directly to exercise the consumption path.
/// </para>
/// <para>
/// Topological sort uses QuikGraph's <see cref="TopologicalSortAlgorithm{TVertex,TEdge}"/>
/// over a <see cref="BidirectionalGraph{TVertex,TEdge}"/> — the canonical engine ordering
/// pattern (mirrors <c>Sparkitect.Stateless.ExecutionGraphBuilder.Resolve()</c>). Cycle
/// detection comes for free via <see cref="NonAcyclicGraphException"/>; deterministic
/// tiebreak is by vertex insertion order. <see cref="Identification"/> is
/// <see cref="IEquatable{T}"/> but not <see cref="IComparable{T}"/>, so a
/// PriorityQueue&lt;Identification, Identification&gt; would not compile — QuikGraph is
/// the engine-wide answer.
/// </para>
/// </remarks>
internal sealed class RenderGraphCompiler
{
    private readonly List<(Identification Id, IPass Pass)> _passes = new();
    private readonly List<(Identification From, Identification To)> _orderingEdges = new();

    public void AddPass(Identification id, IPass pass)
    {
        _passes.Add((id, pass));
    }

    /// <summary>
    /// RG-22 internal seam (D-D3). No public surface at Phase 49 — Phase 53 will add one
    /// and route contributions here.
    /// </summary>
    internal void AddOrderingEdgeInternal(Identification from, Identification to)
    {
        _orderingEdges.Add((from, to));
    }

    public CompiledRenderGraph Compile()
    {
        if (_passes.Count == 0)
            throw new InvalidOperationException(
                "Render graph has no passes — at least one pass must be added before Compile().");

        // Build pass-id -> instance map (preserves insertion order for the ordered output).
        var instanceById = new Dictionary<Identification, IPass>(_passes.Count);
        foreach (var (id, pass) in _passes) instanceById[id] = pass;

        // Construct BidirectionalGraph; vertices added in insertion order so QuikGraph's
        // tiebreak preserves "added-first wins among equally-ready vertices" — same shape
        // as ExecutionGraphBuilder.Resolve().
        var graph = new BidirectionalGraph<Identification, Edge<Identification>>(
            allowParallelEdges: true,
            vertexCapacity: _passes.Count,
            edgeCapacity: _orderingEdges.Count);
        foreach (var (id, _) in _passes)
            graph.AddVertex(id);

        // Validate edges fail-fast: unknown 'from' or 'to' both throw "unknown pass".
        foreach (var (from, to) in _orderingEdges)
        {
            if (!graph.ContainsVertex(from))
                throw new InvalidOperationException(
                    $"Render graph ordering edge references unknown pass '{from}' as 'from'.");
            if (!graph.ContainsVertex(to))
                throw new InvalidOperationException(
                    $"Render graph ordering edge references unknown pass '{to}' as 'to'.");
            graph.AddEdge(new Edge<Identification>(from, to));
        }

        // Cycle detection via QuikGraph (engine canonical pattern — mirrors
        // ExecutionGraphBuilder). NonAcyclicGraphException maps to InvalidOperationException
        // with "cycle" in the message. We use TopologicalSortAlgorithm purely for cycle
        // detection: its DFS-based output order does NOT preserve vertex-insertion-order
        // tiebreak when multiple vertices are unconstrained. The actual ordering below
        // uses Kahn's algorithm so unconstrained passes appear in AddPass-call order
        // (the deterministic-insertion-order requirement per RG-20).
        var algorithm = new TopologicalSortAlgorithm<Identification, Edge<Identification>>(graph);
        try
        {
            algorithm.Compute();
        }
        catch (NonAcyclicGraphException)
        {
            throw new InvalidOperationException(
                "Render graph cycle detected — passes cannot be topologically ordered.");
        }

        // Kahn's algorithm with insertion-order tiebreak. Initialize in-degrees and a
        // ready-queue ordered by AddPass insertion index. When multiple passes are ready
        // simultaneously, the one inserted first wins — yielding the deterministic
        // insertion order documented in must_haves truth #2.
        var inDegree = new Dictionary<Identification, int>(_passes.Count);
        foreach (var (id, _) in _passes)
            inDegree[id] = graph.InDegree(id);

        var ordered = new List<(Identification Id, IPass Pass)>(_passes.Count);
        // Walk passes in insertion order; whenever we find one that is ready (in-degree 0),
        // emit it and decrement in-degrees of its successors. Repeat until done.
        // This is Kahn's with stable insertion-order tiebreak.
        var emitted = new HashSet<Identification>(_passes.Count);
        while (emitted.Count < _passes.Count)
        {
            var progressed = false;
            foreach (var (id, _) in _passes)
            {
                if (emitted.Contains(id)) continue;
                if (inDegree[id] != 0) continue;

                ordered.Add((id, instanceById[id]));
                emitted.Add(id);
                progressed = true;

                foreach (var outEdge in graph.OutEdges(id))
                    inDegree[outEdge.Target]--;
            }
            if (!progressed)
            {
                // Defensive: TopologicalSortAlgorithm above already proved acyclic, so we
                // should always make progress. Reaching here would indicate an internal
                // invariant violation (e.g. parallel edges miscounted) — fail-fast.
                throw new InvalidOperationException(
                    "Render graph cycle detected — passes cannot be topologically ordered.");
            }
        }

        return new CompiledRenderGraph(ordered);
    }
}
