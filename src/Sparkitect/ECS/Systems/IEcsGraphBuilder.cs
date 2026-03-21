using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

public interface IEcsGraphBuilder
{
    void AddNode(Identification systemId, Identification groupId);
    void AddEdge(Identification from, Identification to, bool optional);
    EcsExecutionGraph Resolve();
}
