using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Builds and topologically sorts a function ordering graph. Scheduling implementations add each
/// participating function as a node, then add ordering edges, then resolve the linear execution order.
/// </summary>
[PublicAPI]
public interface IExecutionGraphBuilder
{
    /// <summary>Adds a function node that must appear in the resolved order.</summary>
    void AddNode(Identification node);

    /// <summary>Adds a "from runs before to" edge; when <paramref name="optional"/>, a missing endpoint drops the edge instead of throwing.</summary>
    void AddEdge(Identification from, Identification to, bool optional);

    /// <summary>Topologically sorts the added nodes and edges. Throws if a required edge is unsatisfied or a cycle exists.</summary>
    IReadOnlyList<Identification> Resolve();
}