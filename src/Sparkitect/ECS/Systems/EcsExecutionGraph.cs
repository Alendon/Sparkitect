using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

public sealed class EcsExecutionGraph
{
    /// <summary>
    /// All nodes (groups + systems) in topological order. Used internally for gate/skip execution.
    /// </summary>
    public IReadOnlyList<Identification> SortedAll { get; }

    /// <summary>
    /// Systems only in topological order. Public view -- does not include group gate nodes.
    /// </summary>
    public IReadOnlyList<Identification> SortedSystems { get; }

    /// <summary>
    /// Set of group Identifications present in the graph.
    /// </summary>
    public IReadOnlySet<Identification> GroupIds { get; }

    /// <summary>
    /// Maps group indices in SortedAll to the index AFTER their last descendant.
    /// Used by ExecuteSystems for gate/skip: if group at index i is inactive, jump to GroupSkipRanges[i].
    /// </summary>
    public IReadOnlyDictionary<int, int> GroupSkipRanges { get; }

    /// <summary>
    /// Maps each node to its parent group ID. Used for ancestry checks.
    /// </summary>
    public IReadOnlyDictionary<Identification, Identification> ParentMap { get; }

    public EcsExecutionGraph(
        IReadOnlyList<Identification> sortedAll,
        IReadOnlyList<Identification> sortedSystems,
        IReadOnlySet<Identification> groupIds,
        IReadOnlyDictionary<int, int> groupSkipRanges,
        IReadOnlyDictionary<Identification, Identification> parentMap)
    {
        SortedAll = sortedAll;
        SortedSystems = sortedSystems;
        GroupIds = groupIds;
        GroupSkipRanges = groupSkipRanges;
        ParentMap = parentMap;
    }
}
