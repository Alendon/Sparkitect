using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

internal class EcsGraphBuilder : IEcsGraphBuilder
{
    private readonly ExecutionGraphBuilder _inner = new();
    private readonly Dictionary<Identification, Identification> _groupMembership = new();

    public void AddNode(Identification systemId, Identification groupId)
    {
        _inner.AddNode(systemId);
        _groupMembership[systemId] = groupId;
    }

    public void AddEdge(Identification from, Identification to, bool optional)
    {
        _inner.AddEdge(from, to, optional);
    }

    public EcsExecutionGraph Resolve()
    {
        var sorted = _inner.Resolve();
        var groups = new HashSet<Identification>(_groupMembership.Values);
        return new EcsExecutionGraph(sorted, _groupMembership, groups);
    }
}
