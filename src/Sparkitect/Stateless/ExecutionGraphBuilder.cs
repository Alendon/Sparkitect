using QuikGraph;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    private readonly Dictionary<Edge<Identification>, bool> _edgesAndOptional = [];
    private readonly HashSet<Identification> _added = [];

    public void AddNode(Identification node)
    {
        _added.Add(node);
    }

    public void AddEdge(Identification from, Identification to, bool optional)
    {
        var edge = new Edge<Identification>(from, to);
        _edgesAndOptional[edge] = optional;
    }

    public IReadOnlyList<Identification> Resolve()
    {
        var graph = new AdjacencyGraph<Identification, Edge<Identification>>(true, _added.Count, _edgesAndOptional.Count);
        graph.AddVertexRange(_added);

        foreach (var (edge, optional) in _edgesAndOptional)
        {
            var sourceExists = graph.ContainsVertex(edge.Source);
            var targetExists = graph.ContainsVertex(edge.Target);

            if (sourceExists && targetExists)
            {
                graph.AddEdge(edge);
            }
            else if (!optional)
            {
                var missing = !sourceExists ? edge.Source : edge.Target;
                throw new InvalidOperationException(
                    $"Required ordering dependency not found: {missing}");
            }
        }

        var sorted = new List<Identification>();
        var algorithm = new TopologicalSortAlgorithm<Identification, Edge<Identification>>(graph);

        try
        {
            algorithm.Compute();
            sorted.AddRange(algorithm.SortedVertices);
        }
        catch (NonAcyclicGraphException)
        {
            throw new InvalidOperationException("Cycle detected in function ordering graph");
        }

        return sorted;
    }
}