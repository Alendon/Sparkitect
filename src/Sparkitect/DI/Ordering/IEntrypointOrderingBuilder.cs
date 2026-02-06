namespace Sparkitect.DI.Ordering;

/// <summary>
/// Builder interface for collecting entrypoint ordering constraints.
/// Nodes are full type names (<see cref="System.Type.FullName"/>).
/// </summary>
public interface IEntrypointOrderingBuilder
{
    /// <summary>
    /// Gets the full type name of the entrypoint currently being processed.
    /// </summary>
    string CurrentTypeName { get; }

    /// <summary>
    /// Adds a directed edge meaning "<paramref name="from"/> executes before <paramref name="to"/>".
    /// </summary>
    /// <param name="from">Full type name of the entrypoint that should execute first.</param>
    /// <param name="to">Full type name of the entrypoint that should execute second.</param>
    void AddEdge(string from, string to);
}
