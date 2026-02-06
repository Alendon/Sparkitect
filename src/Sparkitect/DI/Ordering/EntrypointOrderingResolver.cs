using QuikGraph;

namespace Sparkitect.DI.Ordering;

/// <summary>
/// Resolves entrypoint ordering using Kahn's algorithm with deterministic lexicographic tiebreaking.
/// Edges referencing nodes not in the graph are silently ignored (all edges are optional).
/// </summary>
internal class EntrypointOrderingResolver
{
    /// <summary>
    /// Produces a deterministic topological ordering of the given nodes, respecting edge constraints.
    /// When multiple nodes have no remaining dependencies, the lexicographically smallest (by <see cref="StringComparer.Ordinal"/>)
    /// is chosen first.
    /// </summary>
    /// <param name="nodes">All entrypoint type names to order.</param>
    /// <param name="edges">Directed edges where Source should execute before Target.</param>
    /// <returns>The nodes in deterministic topological order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a cycle is detected in the ordering graph.</exception>
    public IReadOnlyList<string> Resolve(IReadOnlyCollection<string> nodes, IReadOnlyCollection<Edge<string>> edges)
    {
        if (nodes.Count <= 1)
        {
            return nodes.ToList();
        }

        var nodeSet = new HashSet<string>(nodes);
        var adjacency = new Dictionary<string, List<string>>(nodes.Count);
        var inDegree = new Dictionary<string, int>(nodes.Count);

        foreach (var node in nodes)
        {
            adjacency[node] = [];
            inDegree[node] = 0;
        }

        foreach (var edge in edges)
        {
            if (!nodeSet.Contains(edge.Source) || !nodeSet.Contains(edge.Target))
            {
                continue;
            }

            adjacency[edge.Source].Add(edge.Target);
            inDegree[edge.Target]++;
        }

        var queue = new PriorityQueue<string, string>(StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            if (inDegree[node] == 0)
            {
                queue.Enqueue(node, node);
            }
        }

        var result = new List<string>(nodes.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;

                if (inDegree[neighbor] == 0)
                {
                    queue.Enqueue(neighbor, neighbor);
                }
            }
        }

        if (result.Count != nodes.Count)
        {
            var cycleNodes = nodes.Where(n => !result.Contains(n));
            throw new InvalidOperationException(
                $"Cycle detected in entrypoint ordering graph. The following types form a cycle: {string.Join(", ", cycleNodes)}");
        }

        return result;
    }
}
