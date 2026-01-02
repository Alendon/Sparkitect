using QuikGraph;

namespace Sparkitect.Stateless;

public class ExecutionGraphBuilder<TNode> : IExecutionGraphBuilder<TNode> where TNode : unmanaged
{
    private Dictionary<Edge<TNode>, bool> _edgesAndOptional = [];
    private HashSet<TNode> _added = [];
    
    
    public void AddNode(TNode node)
    {
        _added.Add(node);
    }

    public void AddEdge(TNode from, TNode to, bool optional)
    {
        var edge = new Edge<TNode>(from, to);
        _edgesAndOptional[edge] = optional;
    }

    public object Resolve()
    {
        AdjacencyGraph<TNode, Edge<TNode>> graph = new(true, _added.Count, _edgesAndOptional.Count);
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