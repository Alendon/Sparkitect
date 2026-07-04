using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;

namespace Sparkitect.Stateless;

internal sealed class ExecutionGraphBuilder : IExecutionGraphBuilder
{
    private readonly OrderingGraphBuilder<Identification> _graph = new();
    private readonly HashSet<Identification> _known = [];

    public void AddNode(Identification node)
    {
        _known.Add(node);
        _graph.AddNode(node);
    }

    public void AddEdge(Identification from, Identification to, bool optional)
    {
        _graph.AddEdge(from, to, optional);
    }

    public IReadOnlyList<Identification> Resolve()
    {
        var result = _graph.Sort(OrderingTiebreak<Identification>.InsertionOrder);

        if (result is Result<IReadOnlyList<Identification>, OrderingError<Identification>>.Ok ok)
        {
            return ok.Value;
        }

        var error = ((Result<IReadOnlyList<Identification>, OrderingError<Identification>>.Error)result).Value;
        throw error switch
        {
            OrderingError<Identification>.MissingRequiredDependency missing =>
                new InvalidOperationException(
                    $"Required ordering dependency not found: {(_known.Contains(missing.From) ? missing.To : missing.From)}"),
            OrderingError<Identification>.Cycle cycle =>
                new InvalidOperationException(
                    $"Cycle detected in function ordering graph: {string.Join(", ", cycle.Participants)}"),
        };
    }
}
