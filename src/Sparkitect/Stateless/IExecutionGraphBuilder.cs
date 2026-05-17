using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

[PublicAPI]
public interface IExecutionGraphBuilder
{
    void AddNode(Identification node);
    void AddEdge(Identification from, Identification to, bool optional);
    IReadOnlyList<Identification> Resolve();
}