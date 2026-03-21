using Serilog;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

internal class EcsGraphBuilder : IEcsGraphBuilder
{
    private readonly ExecutionGraphBuilder _inner = new();
    private readonly HashSet<Identification> _groupIds = new();
    private readonly Dictionary<Identification, Identification> _parentMap = new(); // childId -> parentGroupId

    public void BuildFromTree(
        SystemTreeNode root,
        IReadOnlyDictionary<Identification, IScheduling> systemMetadata,
        IReadOnlyDictionary<Identification, SystemGroupScheduling> groupMetadata)
    {
        WalkNode(root, parentGroupId: null, systemMetadata, groupMetadata);
    }

    private void WalkNode(
        SystemTreeNode node,
        Identification? parentGroupId,
        IReadOnlyDictionary<Identification, IScheduling> systemMetadata,
        IReadOnlyDictionary<Identification, SystemGroupScheduling> groupMetadata)
    {
        _inner.AddNode(node.Id);

        if (node.IsGroup)
        {
            _groupIds.Add(node.Id);

            if (parentGroupId is not null)
            {
                _parentMap[node.Id] = parentGroupId.Value;
                // Implicit edge: parent group -> child group (parent sorts before child)
                _inner.AddEdge(parentGroupId.Value, node.Id, optional: false);
            }

            // Apply group ordering constraints
            if (groupMetadata.TryGetValue(node.Id, out var groupSched))
            {
                ApplyOrderingEdges(node.Id, groupSched.OrderAfter, groupSched.OrderBefore, parentGroupId);
            }

            // Recurse children
            foreach (var child in node.Children)
            {
                WalkNode(child, node.Id, systemMetadata, groupMetadata);
            }
        }
        else
        {
            // System node (leaf)
            if (parentGroupId is not null)
            {
                _parentMap[node.Id] = parentGroupId.Value;
                // Implicit edge: parent group -> system (parent sorts before system)
                _inner.AddEdge(parentGroupId.Value, node.Id, optional: false);
            }

            // Apply system ordering constraints
            if (systemMetadata.TryGetValue(node.Id, out var sched) && sched is EcsSystemScheduling ess)
            {
                ApplyOrderingEdges(node.Id, ess.OrderAfter, ess.OrderBefore, parentGroupId);
            }
        }
    }

    private void ApplyOrderingEdges(
        Identification nodeId,
        IReadOnlyList<OrderAfterAttribute> orderAfter,
        IReadOnlyList<OrderBeforeAttribute> orderBefore,
        Identification? parentGroupId)
    {
        foreach (var after in orderAfter)
        {
            // Cross-group check: warn and drop
            if (IsCrossGroupEdge(nodeId, after.Other, parentGroupId))
            {
                Log.Warning(
                    "Cross-group ordering constraint dropped: {NodeId} OrderAfter {OtherId} - nodes are not siblings",
                    nodeId, after.Other);
                continue;
            }
            _inner.AddEdge(after.Other, nodeId, after.Optional);
        }

        foreach (var before in orderBefore)
        {
            if (IsCrossGroupEdge(nodeId, before.Other, parentGroupId))
            {
                Log.Warning(
                    "Cross-group ordering constraint dropped: {NodeId} OrderBefore {OtherId} - nodes are not siblings",
                    nodeId, before.Other);
                continue;
            }
            _inner.AddEdge(nodeId, before.Other, before.Optional);
        }
    }

    private bool IsCrossGroupEdge(Identification nodeId, Identification otherId, Identification? nodeParentGroupId)
    {
        // If the other node has a known parent and it differs from this node's parent, it's cross-group
        if (nodeParentGroupId is null) return false;
        if (!_parentMap.TryGetValue(otherId, out var otherParent)) return false;
        return otherParent != nodeParentGroupId.Value;
    }

    public EcsExecutionGraph Resolve()
    {
        var sortedAll = _inner.Resolve();

        // Build group skip ranges: for each group at index i, find the index AFTER its last descendant
        var groupSkipRanges = new Dictionary<int, int>();
        var sortedSystems = new List<Identification>(); // Public view: systems only

        // Map Identification -> index in sorted list
        var idToIndex = new Dictionary<Identification, int>();
        for (int i = 0; i < sortedAll.Count; i++)
            idToIndex[sortedAll[i]] = i;

        // Build sorted systems list (excludes groups)
        for (int i = 0; i < sortedAll.Count; i++)
        {
            if (!_groupIds.Contains(sortedAll[i]))
                sortedSystems.Add(sortedAll[i]);
        }

        // For each group, find the furthest descendant index
        foreach (var groupId in _groupIds)
        {
            if (!idToIndex.TryGetValue(groupId, out var groupIndex)) continue;

            int maxDescendantIndex = groupIndex;
            for (int i = groupIndex + 1; i < sortedAll.Count; i++)
            {
                if (IsDescendantOf(sortedAll[i], groupId))
                    maxDescendantIndex = i;
            }
            // Skip-to = one past the last descendant
            groupSkipRanges[groupIndex] = maxDescendantIndex + 1;
        }

        return new EcsExecutionGraph(sortedAll, sortedSystems, _groupIds, groupSkipRanges, _parentMap);
    }

    private bool IsDescendantOf(Identification nodeId, Identification ancestorId)
    {
        var current = nodeId;
        while (_parentMap.TryGetValue(current, out var parent))
        {
            if (parent == ancestorId) return true;
            current = parent;
        }
        return false;
    }
}
