using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

public sealed class EcsExecutionGraph
{
    public IReadOnlyList<Identification> SortedSystems { get; }
    public IReadOnlyDictionary<Identification, Identification> GroupMembership { get; }
    public IReadOnlySet<Identification> Groups { get; }

    public EcsExecutionGraph(
        IReadOnlyList<Identification> sortedSystems,
        IReadOnlyDictionary<Identification, Identification> groupMembership,
        IReadOnlySet<Identification> groups)
    {
        SortedSystems = sortedSystems;
        GroupMembership = groupMembership;
        Groups = groups;
    }
}
