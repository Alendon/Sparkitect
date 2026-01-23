using Sparkitect.Modding;

namespace Sparkitect.Stateless;

public interface IExecutionGraphBuilder
{
    void AddNode(Identification node);
    void AddEdge(Identification from, Identification to, bool optional);
    IReadOnlyList<Identification> Resolve();
}