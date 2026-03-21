using Sparkitect.ECS.Systems;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class EcsGraphBuilderTests
{
    private static readonly Identification GroupA = Identification.Create(1, 1, 1);
    private static readonly Identification GroupB = Identification.Create(1, 1, 2);

    private static readonly Identification System1 = Identification.Create(1, 2, 1);
    private static readonly Identification System2 = Identification.Create(1, 2, 2);
    private static readonly Identification System3 = Identification.Create(1, 2, 3);

    [Test]
    public async Task SingleNode_ProducesSortedListWithOneEntry()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);

        var graph = builder.Resolve();

        await Assert.That(graph.SortedSystems).HasCount().EqualTo(1);
        await Assert.That(graph.SortedSystems[0]).IsEqualTo(System1);
    }

    [Test]
    public async Task SingleNode_GroupMembershipMapsToGroup()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);

        var graph = builder.Resolve();

        await Assert.That(graph.GroupMembership.ContainsKey(System1)).IsTrue();
        await Assert.That(graph.GroupMembership[System1]).IsEqualTo(GroupA);
    }

    [Test]
    public async Task ThreeNodes_WithEdges_ProducesCorrectTopologicalOrder()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);
        builder.AddNode(System2, GroupA);
        builder.AddNode(System3, GroupA);
        // System1 -> System2 -> System3
        builder.AddEdge(System1, System2, false);
        builder.AddEdge(System2, System3, false);

        var graph = builder.Resolve();

        await Assert.That(graph.SortedSystems).HasCount().EqualTo(3);
        var idx1 = graph.SortedSystems.ToList().IndexOf(System1);
        var idx2 = graph.SortedSystems.ToList().IndexOf(System2);
        var idx3 = graph.SortedSystems.ToList().IndexOf(System3);
        await Assert.That(idx1).IsLessThan(idx2);
        await Assert.That(idx2).IsLessThan(idx3);
    }

    [Test]
    public async Task Resolve_GroupMembership_MapsEachSystemToGroup()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);
        builder.AddNode(System2, GroupB);

        var graph = builder.Resolve();

        await Assert.That(graph.GroupMembership[System1]).IsEqualTo(GroupA);
        await Assert.That(graph.GroupMembership[System2]).IsEqualTo(GroupB);
    }

    [Test]
    public async Task Resolve_Groups_ContainsAllDistinctGroupIds()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);
        builder.AddNode(System2, GroupB);
        builder.AddNode(System3, GroupA);

        var graph = builder.Resolve();

        await Assert.That(graph.Groups).HasCount().EqualTo(2);
        await Assert.That(graph.Groups.Contains(GroupA)).IsTrue();
        await Assert.That(graph.Groups.Contains(GroupB)).IsTrue();
    }

    [Test]
    public async Task CycleDetection_ThrowsInvalidOperationException()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);
        builder.AddNode(System2, GroupA);
        builder.AddEdge(System1, System2, false);
        builder.AddEdge(System2, System1, false);

        await Assert.That(() => builder.Resolve()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task OptionalEdge_MissingNode_DoesNotThrow()
    {
        var builder = new EcsGraphBuilder();
        builder.AddNode(System1, GroupA);
        var missingSystem = Identification.Create(99, 99, 99);
        builder.AddEdge(System1, missingSystem, true);

        var graph = builder.Resolve();

        await Assert.That(graph.SortedSystems).HasCount().EqualTo(1);
    }
}
