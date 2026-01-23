using QuikGraph;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    private Dictionary<Edge<Identification>, bool> _edgesAndOptional = [];
    private HashSet<Identification> _added = [];
    
    
    public void AddNode(Identification node)
    {
        _added.Add(node);
    }

    public void AddEdge(Identification from, Identification to, bool optional)
    {
        var edge = new Edge<Identification>(from, to);
        _edgesAndOptional[edge] = optional;
    }

    public object Resolve()
    {
        AdjacencyGraph<Identification, Edge<Identification>> graph = new(true, _added.Count, _edgesAndOptional.Count);
        graph.AddVertexRange(_added);
        graph.AddEdgeRange(_edgesAndOptional.Where(x =>
        {
            var (edge, optional) = x;
            if (graph.ContainsVertex(edge.Source) && graph.ContainsVertex(edge.Target)) return true;
            if (optional) return false;
            throw ...;
        }));

        return graph;
    }
}