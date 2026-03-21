using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Tests.ECS;

public class EcsGraphBuilderTests
{
    private static readonly Identification GroupA = Identification.Create(1, 1, 1);
    private static readonly Identification GroupB = Identification.Create(1, 1, 2);

    private static readonly Identification System1 = Identification.Create(1, 2, 1);
    private static readonly Identification System2 = Identification.Create(1, 2, 2);
    private static readonly Identification System3 = Identification.Create(1, 2, 3);

    [Test]
    public async Task SingleNode_ProducesSortedListWithOneSystem()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        builder.BuildFromTree(root, CreateSystemMeta([(System1, GroupA)]), CreateGroupMeta([(GroupA, null)]));

        var graph = builder.Resolve();

        await Assert.That(graph.SortedSystems).HasCount().EqualTo(1);
        await Assert.That(graph.SortedSystems[0]).IsEqualTo(System1);
    }

    [Test]
    public async Task SingleNode_ParentMapMapsSystemToGroup()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        builder.BuildFromTree(root, CreateSystemMeta([(System1, GroupA)]), CreateGroupMeta([(GroupA, null)]));

        var graph = builder.Resolve();

        await Assert.That(graph.ParentMap.ContainsKey(System1)).IsTrue();
        await Assert.That(graph.ParentMap[System1]).IsEqualTo(GroupA);
    }

    [Test]
    public async Task ThreeNodes_WithEdges_ProducesCorrectTopologicalOrder()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        root.Children.Add(new SystemTreeNode(System3, isGroup: false));

        // System1 -> System2 -> System3 via OrderAfter edges in metadata
        var systemMeta = new Dictionary<Identification, IScheduling>
        {
            [System1] = MakeScheduling(System1, GroupA, [], [new TestOrderBeforeAttribute(System2)]),
            [System2] = MakeScheduling(System2, GroupA, [new TestOrderAfterAttribute(System1)], [new TestOrderBeforeAttribute(System3)]),
            [System3] = MakeScheduling(System3, GroupA, [new TestOrderAfterAttribute(System2)], [])
        };
        builder.BuildFromTree(root, systemMeta, CreateGroupMeta([(GroupA, null)]));

        var graph = builder.Resolve();

        await Assert.That(graph.SortedSystems).HasCount().EqualTo(3);
        var idx1 = graph.SortedSystems.ToList().IndexOf(System1);
        var idx2 = graph.SortedSystems.ToList().IndexOf(System2);
        var idx3 = graph.SortedSystems.ToList().IndexOf(System3);
        await Assert.That(idx1).IsLessThan(idx2);
        await Assert.That(idx2).IsLessThan(idx3);
    }

    [Test]
    public async Task Resolve_ParentMap_MapsEachSystemToGroup()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        var groupB = new SystemTreeNode(GroupB, isGroup: true);
        groupB.Children.Add(new SystemTreeNode(System2, isGroup: false));
        root.Children.Add(groupB);

        builder.BuildFromTree(root,
            CreateSystemMeta([(System1, GroupA), (System2, GroupB)]),
            CreateGroupMeta([(GroupA, null), (GroupB, GroupA)]));

        var graph = builder.Resolve();

        await Assert.That(graph.ParentMap[System1]).IsEqualTo(GroupA);
        await Assert.That(graph.ParentMap[System2]).IsEqualTo(GroupB);
    }

    [Test]
    public async Task Resolve_GroupIds_ContainsAllDistinctGroupIds()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        var groupB = new SystemTreeNode(GroupB, isGroup: true);
        groupB.Children.Add(new SystemTreeNode(System2, isGroup: false));
        groupB.Children.Add(new SystemTreeNode(System3, isGroup: false));
        root.Children.Add(groupB);

        builder.BuildFromTree(root,
            CreateSystemMeta([(System1, GroupA), (System2, GroupB), (System3, GroupB)]),
            CreateGroupMeta([(GroupA, null), (GroupB, GroupA)]));

        var graph = builder.Resolve();

        await Assert.That(graph.GroupIds).HasCount().EqualTo(2);
        await Assert.That(graph.GroupIds.Contains(GroupA)).IsTrue();
        await Assert.That(graph.GroupIds.Contains(GroupB)).IsTrue();
    }

    [Test]
    public async Task Resolve_GroupSkipRanges_ContainsEntryForEachGroup()
    {
        var builder = new EcsGraphBuilder();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));

        builder.BuildFromTree(root,
            CreateSystemMeta([(System1, GroupA)]),
            CreateGroupMeta([(GroupA, null)]));

        var graph = builder.Resolve();

        // GroupA should have a skip range entry
        var groupAIndex = graph.SortedAll.ToList().IndexOf(GroupA);
        await Assert.That(graph.GroupSkipRanges.ContainsKey(groupAIndex)).IsTrue();
        // Skip range should be past System1
        await Assert.That(graph.GroupSkipRanges[groupAIndex]).IsGreaterThan(groupAIndex);
    }

    // --- Helpers ---

    private static Dictionary<Identification, IScheduling> CreateSystemMeta(
        (Identification systemId, Identification groupId)[] entries)
    {
        var dict = new Dictionary<Identification, IScheduling>();
        foreach (var (systemId, groupId) in entries)
        {
            var sched = new EcsSystemScheduling([], []);
            sched.OwnerId = groupId;
            dict[systemId] = sched;
        }
        return dict;
    }

    private static EcsSystemScheduling MakeScheduling(
        Identification systemId, Identification groupId,
        OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        var sched = new EcsSystemScheduling(orderAfter, orderBefore);
        sched.OwnerId = groupId;
        return sched;
    }

    private static Dictionary<Identification, SystemGroupScheduling> CreateGroupMeta(
        (Identification groupId, Identification? parentId)[] entries)
    {
        var dict = new Dictionary<Identification, SystemGroupScheduling>();
        foreach (var (groupId, parentId) in entries)
        {
            ParentIdAttribute? parent = parentId.HasValue
                ? new TestParentIdAttribute(parentId.Value)
                : null;
            dict[groupId] = new SystemGroupScheduling([], [], parent);
        }
        return dict;
    }

    private sealed class TestParentIdAttribute(Identification other) : ParentIdAttribute
    {
        public override Identification Other => other;
    }

    private sealed class TestOrderAfterAttribute(Identification other) : OrderAfterAttribute
    {
        public override Identification Other => other;
        public override bool Optional => false;
    }

    private sealed class TestOrderBeforeAttribute(Identification other) : OrderBeforeAttribute
    {
        public override Identification Other => other;
        public override bool Optional => false;
    }
}
