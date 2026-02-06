using QuikGraph;

namespace Sparkitect.DI.Ordering;

/// <summary>
/// Collects entrypoint ordering constraints as directed edges.
/// Used internally to process <see cref="IEntrypointOrdering"/> attributes on entrypoint types.
/// </summary>
internal class EntrypointOrderingBuilder : IEntrypointOrderingBuilder
{
    private string _currentTypeName = string.Empty;
    private readonly List<Edge<string>> _edges = [];

    /// <inheritdoc />
    public string CurrentTypeName => _currentTypeName;

    /// <summary>
    /// Gets the collected edges as a read-only collection.
    /// </summary>
    public IReadOnlyCollection<Edge<string>> Edges => _edges;

    /// <summary>
    /// Sets the current type being processed. Called before processing each type's ordering attributes.
    /// </summary>
    /// <param name="fullTypeName">The full type name of the entrypoint being processed.</param>
    public void SetCurrentType(string fullTypeName)
    {
        _currentTypeName = fullTypeName;
    }

    /// <inheritdoc />
    public void AddEdge(string from, string to)
    {
        _edges.Add(new Edge<string>(from, to));
    }
}
