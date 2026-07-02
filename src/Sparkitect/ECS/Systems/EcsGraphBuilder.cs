using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

internal class EcsGraphBuilder : IEcsGraphBuilder
{
    private readonly HashSet<Identification> _groupIds = new();
    private readonly Dictionary<Identification, Identification> _parentMap = new(); // childId -> parentGroupId
    private SystemTreeNode? _root;
    private IReadOnlyDictionary<Identification, IScheduling>? _systemMetadata;
    private IReadOnlyDictionary<Identification, SystemGroupScheduling>? _groupMetadata;

    public void BuildFromTree(
        SystemTreeNode root,
        IReadOnlyDictionary<Identification, IScheduling> systemMetadata,
        IReadOnlyDictionary<Identification, SystemGroupScheduling> groupMetadata)
    {
        _root = root;
        _systemMetadata = systemMetadata;
        _groupMetadata = groupMetadata;

        IndexNode(root, parentGroupId: null);
        ValidateConstraints(root, parentGroupId: null);
    }

    private void IndexNode(SystemTreeNode node, Identification? parentGroupId)
    {
        if (node.IsGroup)
            _groupIds.Add(node.Id);
        if (parentGroupId is not null)
            _parentMap[node.Id] = parentGroupId.Value;

        foreach (var child in node.Children)
            IndexNode(child, node.Id);
    }

    // Ordering constraints may only reference siblings. The full tree is indexed before validation,
    // so forward references to nodes in later-walked groups are caught too.
    private void ValidateConstraints(SystemTreeNode node, Identification? parentGroupId)
    {
        if (parentGroupId is not null)
        {
            var (orderAfter, orderBefore) = ConstraintsFor(node);
            foreach (var after in orderAfter)
                ThrowIfCrossGroup(node.Id, after.Other, parentGroupId.Value, "OrderAfter");
            foreach (var before in orderBefore)
                ThrowIfCrossGroup(node.Id, before.Other, parentGroupId.Value, "OrderBefore");
        }

        foreach (var child in node.Children)
            ValidateConstraints(child, node.Id);
    }

    private void ThrowIfCrossGroup(
        Identification nodeId, Identification otherId, Identification parentGroupId, string verb)
    {
        // Targets outside the tree are handled by edge optionality at the local sort.
        if (!_parentMap.TryGetValue(otherId, out var otherParent))
            return;
        if (otherParent != parentGroupId)
            throw new InvalidOperationException(
                $"Cross-group ordering constraint: {nodeId} {verb} {otherId} - nodes are not siblings in the same group.");
    }

    public EcsExecutionGraph Resolve()
    {
        if (_root is null)
            throw new InvalidOperationException("BuildFromTree must be called before Resolve.");

        var sortedAll = new List<Identification>();
        var groupSkipRanges = new Dictionary<int, int>();
        AppendGroupSpan(_root, sortedAll, groupSkipRanges);

        var sortedSystems = new List<Identification>();
        foreach (var id in sortedAll)
        {
            if (!_groupIds.Contains(id))
                sortedSystems.Add(id);
        }

        return new EcsExecutionGraph(sortedAll, sortedSystems, _groupIds, groupSkipRanges, _parentMap);
    }

    // Each group sorts its DIRECT children locally and splices child spans depth-first: group members
    // stay contiguous, ordering against a sibling group orders against its whole span, and gate skip
    // ranges are exact by construction.
    private void AppendGroupSpan(
        SystemTreeNode group, List<Identification> sortedAll, Dictionary<int, int> skipRanges)
    {
        var gateIndex = sortedAll.Count;
        sortedAll.Add(group.Id);

        var local = new ExecutionGraphBuilder();
        var childrenById = new Dictionary<Identification, SystemTreeNode>();
        foreach (var child in group.Children)
        {
            childrenById[child.Id] = child;
            local.AddNode(child.Id);
        }

        foreach (var child in group.Children)
        {
            var (orderAfter, orderBefore) = ConstraintsFor(child);
            foreach (var after in orderAfter)
                local.AddEdge(after.Other, child.Id, after.Optional);
            foreach (var before in orderBefore)
                local.AddEdge(child.Id, before.Other, before.Optional);
        }

        foreach (var id in local.Resolve())
        {
            var child = childrenById[id];
            if (child.IsGroup)
                AppendGroupSpan(child, sortedAll, skipRanges);
            else
                sortedAll.Add(child.Id);
        }

        // Skip-to = one past the group's whole span.
        skipRanges[gateIndex] = sortedAll.Count;
    }

    private (IReadOnlyList<OrderAfterAttribute> After, IReadOnlyList<OrderBeforeAttribute> Before)
        ConstraintsFor(SystemTreeNode node)
    {
        if (node.IsGroup)
        {
            return _groupMetadata!.TryGetValue(node.Id, out var groupSched)
                ? (groupSched.OrderAfter, groupSched.OrderBefore)
                : ([], []);
        }

        return _systemMetadata!.TryGetValue(node.Id, out var sched) && sched is EcsSystemScheduling ess
            ? (ess.OrderAfter, ess.OrderBefore)
            : ([], []);
    }
}
