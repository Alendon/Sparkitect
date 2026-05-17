using QuikGraph;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Pure-logic graph builder + compiler owned by <see cref="RenderGraph"/>. Topological
/// sort uses QuikGraph for cycle detection and Kahn's algorithm with insertion-order
/// tiebreak so unconstrained passes emit in <see cref="AddPass"/> call order.
/// </summary>
internal sealed class RenderGraphCompiler
{
    private readonly List<(Identification Id, IPass Pass)> _passes = new();
    private readonly List<(Identification From, Identification To)> _orderingEdges = new();

    public void AddPass(Identification id, IPass pass)
    {
        _passes.Add((id, pass));
    }

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

        // Cycle detection only — TopologicalSortAlgorithm's DFS output doesn't preserve
        // insertion-order tiebreak; the ordering pass below (Kahn's) does.
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

        // Kahn's algorithm with insertion-order tiebreak: among equally-ready passes,
        // the one added first wins.
        var inDegree = new Dictionary<Identification, int>(_passes.Count);
        foreach (var (id, _) in _passes)
            inDegree[id] = graph.InDegree(id);

        var ordered = new List<(Identification Id, IPass Pass)>(_passes.Count);
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
                // Defensive — TopologicalSortAlgorithm above already proved acyclic.
                throw new InvalidOperationException(
                    "Render graph cycle detected — passes cannot be topologically ordered.");
            }
        }

        return new CompiledRenderGraph(ordered);
    }
}
