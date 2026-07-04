using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;

namespace Sparkitect.DI.Ordering;

/// <summary>
/// Resolves entrypoint ordering by delegating to the shared <see cref="OrderingGraphBuilder{TNode}"/>
/// core under the lexicographic (ordinal) tiebreak. Every edge is optional: an edge referencing a node
/// not present in the graph is silently dropped, preserving the all-optional cross-mod ordering semantics.
/// </summary>
internal class EntrypointOrderingResolver
{
    /// <summary>
    /// Produces a deterministic topological ordering of the given nodes, respecting edge constraints.
    /// When multiple nodes have no remaining dependencies, the lexicographically smallest (by
    /// <see cref="StringComparer.Ordinal"/>) is chosen first.
    /// </summary>
    /// <param name="nodes">All entrypoint type names to order.</param>
    /// <param name="edges">Directed edges where <c>From</c> should execute before <c>To</c>.</param>
    /// <returns>The nodes in deterministic topological order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a cycle is detected in the ordering graph.</exception>
    public IReadOnlyList<string> Resolve(
        IReadOnlyCollection<string> nodes,
        IReadOnlyCollection<(string From, string To)> edges)
    {
        var graph = new OrderingGraphBuilder<string>();

        foreach (var node in nodes)
        {
            graph.AddNode(node);
        }

        foreach (var (from, to) in edges)
        {
            graph.AddEdge(from, to, optional: true);
        }

        var result = graph.Sort(OrderingTiebreak<string>.Lexicographic(StringComparer.Ordinal));

        if (result is not Result<IReadOnlyList<string>, OrderingError<string>>.Ok ok)
        {
            var error = ((Result<IReadOnlyList<string>, OrderingError<string>>.Error)result).Value;
            throw ToException(error);
        }

        return ok.Value;
    }

    private static InvalidOperationException ToException(OrderingError<string> error) => error switch
    {
        OrderingError<string>.Cycle cycle => new InvalidOperationException(
            $"Cycle detected in entrypoint ordering graph. The following types form a cycle: {string.Join(", ", cycle.Participants)}"),
        OrderingError<string>.MissingRequiredDependency missing => new InvalidOperationException(
            $"Entrypoint ordering failed: required dependency {missing.From} -> {missing.To} is missing."),
    };
}
