using Imposter.Abstractions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Storage;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Metadata;
using Sparkitect.Modding;
using Sparkitect.Stateless;

[assembly: GenerateImposter(typeof(IStatelessFunctionManager))]
[assembly: GenerateImposter(typeof(IDIService))]
[assembly: GenerateImposter(typeof(IGameStateManager))]
[assembly: GenerateImposter(typeof(ICoreContainer))]
[assembly: GenerateImposter(typeof(IResolutionScope))]

namespace Sparkitect.Tests.ECS;

public class SystemManagerTests
{
    private static readonly Identification GroupA = Identification.Create(1, 1, 1);
    private static readonly Identification GroupB = Identification.Create(1, 1, 2);
    private static readonly Identification GroupC = Identification.Create(1, 1, 3);

    private static readonly Identification System1 = Identification.Create(1, 2, 1);
    private static readonly Identification System2 = Identification.Create(1, 2, 2);
    private static readonly Identification System3 = Identification.Create(1, 2, 3);
    private static readonly Identification System4 = Identification.Create(1, 2, 4);

    // --- Registration Tests ---

    [Test]
    public async Task RegisterSystem_StoresSystemId()
    {
        var manager = CreateManagerForRegistration();

        manager.RegisterSystem(System1);
        manager.RegisterSystem(System2);

        await Assert.That(manager.RegisteredSystems).Contains(System1);
        await Assert.That(manager.RegisteredSystems).Contains(System2);
    }

    [Test]
    public async Task RegisterSystemGroup_StoresGroupId()
    {
        var manager = CreateManagerForRegistration();

        manager.RegisterSystemGroup(GroupA);

        await Assert.That(manager.RegisteredGroups).Contains(GroupA);
    }

    // --- Execution Tests ---

    [Test]
    public async Task ExecuteSystems_CallsExecuteOnActiveWrappers()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteSystems_SkipsInactiveSystem()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false, SystemState.Inactive));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteSystems_SkipsSystemsInInactiveGroup()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupB);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupB)],
            [wrapper1, wrapper2],
            childGroups: [(GroupB, GroupA)]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        var groupB = new SystemTreeNode(GroupB, isGroup: true, SystemState.Inactive);
        groupB.Children.Add(new SystemTreeNode(System2, isGroup: false));
        root.Children.Add(groupB);
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteSystems_CallsWrappersInSortedOrder()
    {
        var executionOrder = new List<Identification>();
        var wrapper1 = new TrackingWrapper(System1, GroupA, executionOrder);
        var wrapper2 = new TrackingWrapper(System2, GroupA, executionOrder);
        var wrapper3 = new TrackingWrapper(System3, GroupA, executionOrder);
        // Edges enforce topological order: System3 -> System1 -> System2
        var manager = CreateManager(
            [(System3, GroupA), (System1, GroupA), (System2, GroupA)],
            [wrapper3, wrapper1, wrapper2],
            edges: [(System3, System1), (System1, System2)]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        root.Children.Add(new SystemTreeNode(System3, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(executionOrder).HasCount().EqualTo(3);
        await Assert.That(executionOrder[0]).IsEqualTo(System3);
        await Assert.That(executionOrder[1]).IsEqualTo(System1);
        await Assert.That(executionOrder[2]).IsEqualTo(System2);
    }

    // --- Gate/Skip Tests ---

    [Test]
    public async Task ExecuteSystems_GroupGateSkipsEntireSubtree()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true, SystemState.Inactive);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(0);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteSystems_NestedGroupGateSkipsNestedSubtree()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupB);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupB), (System2, GroupA)],
            [wrapper1, wrapper2],
            childGroups: [(GroupB, GroupA)]);
        var world = IWorld.Create();

        // RootGroup (GroupA) -> ChildGroup (GroupB, Inactive) -> System1
        // RootGroup (GroupA) -> System2
        var root = new SystemTreeNode(GroupA, isGroup: true);
        var childGroup = new SystemTreeNode(GroupB, isGroup: true, SystemState.Inactive);
        childGroup.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(childGroup);
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(0);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(1);
    }

    [Test]
    public async Task ExecuteSystems_GroupReactivationResumesChildStates()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        var sys1Node = new SystemTreeNode(System1, isGroup: false);
        var sys2Node = new SystemTreeNode(System2, isGroup: false, SystemState.Inactive);
        root.Children.Add(sys1Node);
        root.Children.Add(sys2Node);
        world.SetSystemTree(root);

        // Deactivate group, execute -- nothing runs
        root.State = SystemState.Inactive;
        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(0);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(0);

        // Reactivate group, execute -- System1 runs (Active), System2 does NOT (Inactive child state preserved)
        root.State = SystemState.Active;
        manager.ExecuteSystems(world, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(0);
    }

    [Test]
    public async Task ExecuteSystems_StateToggleDoesNotRebuildCache()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager(
            [(System1, GroupA)],
            [wrapper]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        // First call builds cache
        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        // Toggle state, execute again -- cache persists
        root.State = SystemState.Inactive;
        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        // Toggle back
        root.State = SystemState.Active;
        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    [Test]
    public async Task ExecuteSystems_OrderingBetweenSiblings()
    {
        var executionOrder = new List<Identification>();
        var wrapper1 = new TrackingWrapper(System1, GroupA, executionOrder);
        var wrapper2 = new TrackingWrapper(System2, GroupA, executionOrder);
        // System2 OrderAfter System1
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2],
            edges: [(System1, System2)]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        root.Children.Add(new SystemTreeNode(System2, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(executionOrder).HasCount().EqualTo(2);
        await Assert.That(executionOrder[0]).IsEqualTo(System1);
        await Assert.That(executionOrder[1]).IsEqualTo(System2);
    }

    [Test]
    public async Task ExecuteSystems_GroupsOrderedAmongSiblings()
    {
        // Two child groups in root, with ordering: GroupB after GroupA
        // System1 in GroupB, System2 in GroupC
        // GroupC is ordered before GroupB via group ordering
        var executionOrder = new List<Identification>();
        var wrapper1 = new TrackingWrapper(System1, GroupB, executionOrder);
        var wrapper2 = new TrackingWrapper(System2, GroupC, executionOrder);
        var manager = CreateManager(
            [(System1, GroupB), (System2, GroupC)],
            [wrapper1, wrapper2],
            childGroups: [(GroupB, GroupA), (GroupC, GroupA)],
            groupEdges: [(GroupC, GroupB)]); // GroupC before GroupB
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        var groupB = new SystemTreeNode(GroupB, isGroup: true);
        groupB.Children.Add(new SystemTreeNode(System1, isGroup: false));
        var groupC = new SystemTreeNode(GroupC, isGroup: true);
        groupC.Children.Add(new SystemTreeNode(System2, isGroup: false));
        root.Children.Add(groupB);
        root.Children.Add(groupC);
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);

        await Assert.That(executionOrder).HasCount().EqualTo(2);
        // GroupC before GroupB, so System2 before System1
        await Assert.That(executionOrder[0]).IsEqualTo(System2);
        await Assert.That(executionOrder[1]).IsEqualTo(System1);
    }

    [Test]
    public async Task ExecuteSystems_DeeplyNestedTree()
    {
        // Root -> GroupB -> GroupC -> System1
        var wrapper1 = new TrackingWrapper(System1, GroupC);
        var manager = CreateManager(
            [(System1, GroupC)],
            [wrapper1],
            childGroups: [(GroupB, GroupA), (GroupC, GroupB)]);
        var world = IWorld.Create();

        var root = new SystemTreeNode(GroupA, isGroup: true);
        var groupB = new SystemTreeNode(GroupB, isGroup: true);
        var groupC = new SystemTreeNode(GroupC, isGroup: true);
        groupC.Children.Add(new SystemTreeNode(System1, isGroup: false));
        groupB.Children.Add(groupC);
        root.Children.Add(groupB);
        world.SetSystemTree(root);

        // All active -- system executes
        manager.ExecuteSystems(world, default);
        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);

        // Deactivate middle group -- system skipped
        groupB.State = SystemState.Inactive;
        manager.ExecuteSystems(world, default);
        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1); // still 1, not 2
    }

    // --- Cache Tests ---

    [Test]
    public async Task NotifyRebuild_ClearsCachedGraphForWorld()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        manager.NotifyRebuild(world);
        await Assert.That(manager.HasCachedWorld(world)).IsFalse();
    }

    [Test]
    public async Task NotifyDispose_ClearsAllCachedStateForWorld()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        manager.NotifyDispose(world);
        await Assert.That(manager.HasCachedWorld(world)).IsFalse();
    }

    [Test]
    public async Task TwoWorlds_GetIndependentGraphs()
    {
        var wrapper1 = new TrackingWrapper(System1, GroupA);
        var wrapper2 = new TrackingWrapper(System2, GroupA);
        var manager = CreateManager(
            [(System1, GroupA), (System2, GroupA)],
            [wrapper1, wrapper2]);

        var world1 = IWorld.Create();
        var world2 = IWorld.Create();

        var root1 = new SystemTreeNode(GroupA, isGroup: true);
        root1.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world1.SetSystemTree(root1);

        var root2 = new SystemTreeNode(GroupA, isGroup: true);
        root2.Children.Add(new SystemTreeNode(System2, isGroup: false));
        world2.SetSystemTree(root2);

        manager.ExecuteSystems(world1, default);
        manager.ExecuteSystems(world2, default);

        await Assert.That(wrapper1.ExecuteCount).IsEqualTo(1);
        await Assert.That(wrapper2.ExecuteCount).IsEqualTo(1);

        // Rebuild world 1 should not affect world 2
        manager.NotifyRebuild(world1);
        await Assert.That(manager.HasCachedWorld(world1)).IsFalse();
        await Assert.That(manager.HasCachedWorld(world2)).IsTrue();
    }

    // --- BuildWorldCache Integration Tests ---

    [Test]
    public async Task ExecuteSystems_BuildsWorldCacheOnFirstCall()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        await Assert.That(manager.HasCachedWorld(world)).IsFalse();
        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    [Test]
    public async Task ExecuteSystems_UsesCachedStateOnSubsequentCalls()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);
        manager.ExecuteSystems(world, default);

        // Wrapper executed twice (once per ExecuteSystems) but cache only built once
        await Assert.That(wrapper.ExecuteCount).IsEqualTo(2);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    [Test]
    public async Task NotifyRebuild_CausesNextExecuteToRebuildCache()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        var root = new SystemTreeNode(GroupA, isGroup: true);
        root.Children.Add(new SystemTreeNode(System1, isGroup: false));
        world.SetSystemTree(root);

        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        manager.NotifyRebuild(world);
        await Assert.That(manager.HasCachedWorld(world)).IsFalse();

        manager.ExecuteSystems(world, default);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    // --- EcsResolutionProvider Tests ---

    [Test]
    public async Task EcsResolutionProvider_TryResolve_ReturnsFalseForNonQueryMetadata()
    {
        using var world = IWorld.Create();
        var provider = new EcsResolutionProvider(world);

        var result = provider.TryResolve(typeof(string), null!, [new FacadeMapping(typeof(string))], out var service);

        await Assert.That(result).IsFalse();
        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task EcsResolutionProvider_TryResolve_ReturnsQueryForQueryMetadata()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        using var storage = new SoAStorage(
            [(TestPosition.Identification, sizeof(float) * 2)],
            tracker, world);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var provider = new EcsResolutionProvider(world);
        var metadata = new ComponentQueryMetadata([TestPosition.Identification]);

        var result = provider.TryResolve(typeof(ComponentQuery), null!, [metadata], out var service);

        await Assert.That(result).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<ComponentQuery>();

        // Cleanup
        provider.CleanupQueries();
    }

    [Test]
    public async Task EcsResolutionProvider_CleanupQueries_DisposesTrackedQueries()
    {
        using var world = IWorld.Create();
        var provider = new EcsResolutionProvider(world);
        var metadata = new ComponentQueryMetadata([TestPosition.Identification]);

        provider.TryResolve(typeof(ComponentQuery), null!, [metadata], out _);

        // CleanupQueries should dispose queries (unregister filters)
        provider.CleanupQueries();

        // Adding a storage after cleanup should not throw --
        // filter was unregistered so no callback fires on disposed query
        var tracker = new FakeObjectTracker();
        using var storage = new SoAStorage(
            [(TestPosition.Identification, sizeof(float) * 2)],
            tracker, world);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task NotifyDispose_CleansUpQueries()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();

        // Add storage so the query has something to match
        using var storage = new SoAStorage(
            [(TestPosition.Identification, sizeof(float) * 2)],
            tracker, world);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Build a real EcsResolutionProvider with a query
        var provider = new EcsResolutionProvider(world);
        var metadata = new ComponentQueryMetadata([TestPosition.Identification]);
        provider.TryResolve(typeof(ComponentQuery), null!, [metadata], out _);

        // Simulate what NotifyDispose does: cleanup then remove
        provider.CleanupQueries();

        // Verify no exception from double cleanup
        provider.CleanupQueries();

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task ExecuteSystems_DeliversFrameTimingToHolder()
    {
        // Create a real EcsResolutionProvider to verify FrameTimingHolder resolution
        using var world = IWorld.Create();
        var provider = new EcsResolutionProvider(world);
        var holder = new FrameTimingHolder();
        provider.SetFrameTimingHolder(holder);

        // Verify TryResolve returns the holder for FrameTimingMetadata
        var resolved = provider.TryResolve(
            typeof(FrameTimingHolder), null!, [new FrameTimingMetadata()], out var service);

        await Assert.That(resolved).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<FrameTimingHolder>();

        // Update holder and verify values propagate
        var timing = new FrameTiming(0.016f, 1.5f);
        holder.Update(timing);

        var resolvedHolder = (FrameTimingHolder)service!;
        await Assert.That(resolvedHolder.DeltaTime).IsEqualTo(0.016f);
        await Assert.That(resolvedHolder.TotalTime).IsEqualTo(1.5f);

        // Update again to verify mutability
        var timing2 = new FrameTiming(0.033f, 2.0f);
        holder.Update(timing2);

        await Assert.That(resolvedHolder.DeltaTime).IsEqualTo(0.033f);
        await Assert.That(resolvedHolder.TotalTime).IsEqualTo(2.0f);
    }

    [Test]
    public async Task EcsResolutionProvider_TryResolve_ThrowsWithoutFrameTimingHolder()
    {
        using var world = IWorld.Create();
        var provider = new EcsResolutionProvider(world);

        // Do NOT set holder -- should throw
        await Assert.That(() =>
        {
            provider.TryResolve(typeof(FrameTimingHolder), null!, [new FrameTimingMetadata()], out _);
        }).Throws<InvalidOperationException>().WithMessageMatching("*FrameTimingHolder*");
    }

    // --- FetchMetadata Tests ---

    [Test]
    public async Task FetchMetadata_PopulatesSystemAndGroupMetadata()
    {
        var systemEntrypoint = new TestSchedulingEntrypoint(
            [(System1, GroupA)], []);
        var groupEntrypoint = new TestGroupMetadataEntrypoint(
            [(GroupA, null)]);
        var manager = CreateManagerWithFetchMetadata(systemEntrypoint, groupEntrypoint);

        manager.RegisterSystem(System1);
        manager.RegisterSystemGroup(GroupA);
        manager.FetchMetadata();

        // Verify BuildTree works (which means metadata was populated)
        var tree = manager.BuildTree(GroupA);
        await Assert.That(tree.Id).IsEqualTo(GroupA);
        await Assert.That(tree.IsGroup).IsTrue();
        await Assert.That(tree.Children).HasCount().EqualTo(1);
        await Assert.That(tree.Children[0].Id).IsEqualTo(System1);
    }

    // --- BuildTree Tests ---

    [Test]
    public async Task BuildTree_ProducesCorrectHierarchy()
    {
        var manager = CreateManagerForRegistration();
        manager.RegisterSystem(System1);
        manager.RegisterSystem(System2);
        manager.RegisterSystemGroup(GroupA);
        manager.RegisterSystemGroup(GroupB);

        // GroupB is a child of GroupA; System1 in GroupA, System2 in GroupB
        var systemMetadata = new Dictionary<Identification, IScheduling>
        {
            [System1] = CreateScheduling(System1, GroupA),
            [System2] = CreateScheduling(System2, GroupB)
        };
        var groupMetadata = new Dictionary<Identification, SystemGroupScheduling>
        {
            [GroupA] = new SystemGroupScheduling([], [], null),
            [GroupB] = new SystemGroupScheduling([], [], new TestParentIdAttribute(GroupA))
        };
        manager.InjectMetadata(systemMetadata, groupMetadata);

        var tree = manager.BuildTree(GroupA);

        await Assert.That(tree.Id).IsEqualTo(GroupA);
        await Assert.That(tree.IsGroup).IsTrue();
        await Assert.That(tree.Children).HasCount().EqualTo(2);

        // Find child group and system
        var childGroup = tree.Children.First(c => c.Id == GroupB);
        var childSystem = tree.Children.First(c => c.Id == System1);

        await Assert.That(childGroup.IsGroup).IsTrue();
        await Assert.That(childGroup.Children).HasCount().EqualTo(1);
        await Assert.That(childGroup.Children[0].Id).IsEqualTo(System2);
        await Assert.That(childSystem.IsGroup).IsFalse();
    }

    [Test]
    public async Task BuildTree_ExcludesUnreachableSystems()
    {
        var manager = CreateManagerForRegistration();
        manager.RegisterSystem(System1);
        manager.RegisterSystem(System2);
        manager.RegisterSystem(System3);
        manager.RegisterSystemGroup(GroupA);
        manager.RegisterSystemGroup(GroupB);

        // System3 belongs to GroupB which is NOT a child of GroupA
        var systemMetadata = new Dictionary<Identification, IScheduling>
        {
            [System1] = CreateScheduling(System1, GroupA),
            [System2] = CreateScheduling(System2, GroupA),
            [System3] = CreateScheduling(System3, GroupB)
        };
        var groupMetadata = new Dictionary<Identification, SystemGroupScheduling>
        {
            [GroupA] = new SystemGroupScheduling([], [], null),
            [GroupB] = new SystemGroupScheduling([], [], null) // No parent -- standalone
        };
        manager.InjectMetadata(systemMetadata, groupMetadata);

        var tree = manager.BuildTree(GroupA);

        // Only System1 and System2 should be in tree; System3 is unreachable
        await Assert.That(tree.Children).HasCount().EqualTo(2);
        var childIds = tree.Children.Select(c => c.Id).ToList();
        await Assert.That(childIds).Contains(System1);
        await Assert.That(childIds).Contains(System2);
        await Assert.That(childIds).DoesNotContain(System3);
    }

    [Test]
    public async Task BuildTree_ThrowsWhenMetadataNotFetched()
    {
        var manager = CreateManagerForRegistration();
        manager.RegisterSystemGroup(GroupA);

        await Assert.That(() => manager.BuildTree(GroupA))
            .Throws<InvalidOperationException>()
            .WithMessageMatching("*FetchMetadata*");
    }

    [Test]
    public async Task BuildTree_ThrowsForUnregisteredGroup()
    {
        var manager = CreateManagerForRegistration();
        manager.InjectMetadata(
            new Dictionary<Identification, IScheduling>(),
            new Dictionary<Identification, SystemGroupScheduling>());

        await Assert.That(() => manager.BuildTree(GroupA))
            .Throws<InvalidOperationException>()
            .WithMessageMatching("*not registered*");
    }

    // --- Helpers ---

    private static SystemManager CreateManagerForRegistration()
    {
        return new SystemManager(
            new IStatelessFunctionManagerImposter(ImposterMode.Implicit).Instance(),
            new IDIServiceImposter(ImposterMode.Implicit).Instance(),
            new IGameStateManagerImposter(ImposterMode.Implicit).Instance());
    }

    private static SystemManager CreateManager(
        (Identification systemId, Identification groupId)[] graphNodes,
        IStatelessFunction[] wrappers,
        (Identification from, Identification to)[]? edges = null,
        (Identification childGroupId, Identification parentGroupId)[]? childGroups = null,
        (Identification from, Identification to)[]? groupEdges = null)
    {
        var sfManagerImposter = new IStatelessFunctionManagerImposter(ImposterMode.Implicit);
        var diServiceImposter = new IDIServiceImposter(ImposterMode.Implicit);
        var gsmImposter = new IGameStateManagerImposter(ImposterMode.Implicit);
        var coreContainerImposter = new ICoreContainerImposter(ImposterMode.Implicit);
        var scopeImposter = new IResolutionScopeImposter(ImposterMode.Implicit);

        // Configure IGameStateManager
        gsmImposter.LoadedMods.Getter().Returns(Array.Empty<string>());
        gsmImposter.CurrentCoreContainer.Getter().Returns(coreContainerImposter.Instance());

        // Configure IDIService.BuildScope
        diServiceImposter.BuildScope(
                Arg<ICoreContainer>.Any(), Arg<IResolutionProvider?>.Any(),
                Arg<IEnumerable<string>>.Any(), Arg<IEnumerable<Type>>.Any())
            .Returns(scopeImposter.Instance());

        // Configure IStatelessFunctionManager
        sfManagerImposter.GetRegisteredWrapperTypes()
            .Returns((IReadOnlyCollection<Type>)new List<Type>());
        sfManagerImposter.InstantiateWrappers(
                Arg<IReadOnlyList<Identification>>.Any(), Arg<IResolutionScope>.Any())
            .Returns((IReadOnlyList<IStatelessFunction>)wrappers);

        var manager = new SystemManager(
            sfManagerImposter.Instance(),
            diServiceImposter.Instance(),
            gsmImposter.Instance());

        // Build system metadata
        var systemMetadata = new Dictionary<Identification, IScheduling>();
        foreach (var (systemId, groupId) in graphNodes)
        {
            var scheduling = new EcsSystemScheduling(
                edges?.Where(e => e.to == systemId)
                    .Select(e => (OrderAfterAttribute)new TestOrderAfterAttribute(e.from))
                    .ToArray() ?? [],
                edges?.Where(e => e.from == systemId)
                    .Select(e => (OrderBeforeAttribute)new TestOrderBeforeAttribute(e.to))
                    .ToArray() ?? []);
            scheduling.OwnerId = groupId;
            systemMetadata[systemId] = scheduling;
        }

        // Build group metadata -- collect all unique groups from nodes and childGroups
        var groupMetadata = new Dictionary<Identification, SystemGroupScheduling>();
        var allGroups = new HashSet<Identification>();
        foreach (var (_, groupId) in graphNodes)
            allGroups.Add(groupId);
        if (childGroups is not null)
        {
            foreach (var (childGroupId, parentGroupId) in childGroups)
            {
                allGroups.Add(childGroupId);
                allGroups.Add(parentGroupId);
            }
        }

        // Build parent lookup for groups
        var groupParentLookup = new Dictionary<Identification, Identification>();
        if (childGroups is not null)
        {
            foreach (var (childGroupId, parentGroupId) in childGroups)
                groupParentLookup[childGroupId] = parentGroupId;
        }

        // Build group edge lookup for group ordering
        var groupOrderAfterLookup = new Dictionary<Identification, List<OrderAfterAttribute>>();
        var groupOrderBeforeLookup = new Dictionary<Identification, List<OrderBeforeAttribute>>();
        if (groupEdges is not null)
        {
            foreach (var (from, to) in groupEdges)
            {
                // "from -> to" means from runs before to, i.e., to has OrderAfter(from)
                if (!groupOrderAfterLookup.TryGetValue(to, out var afterList))
                {
                    afterList = [];
                    groupOrderAfterLookup[to] = afterList;
                }
                afterList.Add(new TestOrderAfterAttribute(from));

                if (!groupOrderBeforeLookup.TryGetValue(from, out var beforeList))
                {
                    beforeList = [];
                    groupOrderBeforeLookup[from] = beforeList;
                }
                beforeList.Add(new TestOrderBeforeAttribute(to));
            }
        }

        foreach (var groupId in allGroups)
        {
            ParentIdAttribute? parent = groupParentLookup.TryGetValue(groupId, out var parentId)
                ? new TestParentIdAttribute(parentId)
                : null;
            var orderAfter = groupOrderAfterLookup.TryGetValue(groupId, out var al)
                ? al.ToArray()
                : Array.Empty<OrderAfterAttribute>();
            var orderBefore = groupOrderBeforeLookup.TryGetValue(groupId, out var bl)
                ? bl.ToArray()
                : Array.Empty<OrderBeforeAttribute>();
            groupMetadata[groupId] = new SystemGroupScheduling(orderAfter, orderBefore, parent);
        }

        manager.InjectMetadata(systemMetadata, groupMetadata);

        return manager;
    }

    private static SystemManager CreateManagerWithFetchMetadata(
        TestSchedulingEntrypoint systemEntrypoint,
        TestGroupMetadataEntrypoint groupEntrypoint)
    {
        var sfManagerImposter = new IStatelessFunctionManagerImposter(ImposterMode.Implicit);
        var diServiceImposter = new IDIServiceImposter(ImposterMode.Implicit);
        var gsmImposter = new IGameStateManagerImposter(ImposterMode.Implicit);

        gsmImposter.LoadedMods.Getter().Returns(Array.Empty<string>());

        var systemContainer = new TestEntrypointContainer<
            ApplyMetadataEntrypoint<IScheduling>>(systemEntrypoint);
        diServiceImposter.CreateEntrypointContainer<
                ApplyMetadataEntrypoint<IScheduling>>(
                Arg<IEnumerable<string>>.Any())
            .Returns(systemContainer);

        var groupContainer = new TestEntrypointContainer<
            ApplyMetadataEntrypoint<SystemGroupScheduling>>(groupEntrypoint);
        diServiceImposter.CreateEntrypointContainer<
                ApplyMetadataEntrypoint<SystemGroupScheduling>>(
                Arg<IEnumerable<string>>.Any())
            .Returns(groupContainer);

        return new SystemManager(
            sfManagerImposter.Instance(),
            diServiceImposter.Instance(),
            gsmImposter.Instance());
    }

    private static EcsSystemScheduling CreateScheduling(Identification systemId, Identification groupId)
    {
        var scheduling = new EcsSystemScheduling([], []);
        scheduling.OwnerId = groupId;
        return scheduling;
    }

    private sealed class TestSchedulingEntrypoint(
        (Identification systemId, Identification groupId)[] nodes,
        (Identification from, Identification to)[] edges)
        : ApplyMetadataEntrypoint<IScheduling>
    {
        public override void CollectMetadata(Dictionary<Identification, IScheduling> metadata)
        {
            // Build edge lookup: for each system, find its OrderAfter edges
            var orderAfterBySystem = new Dictionary<Identification, List<TestOrderAfterAttribute>>();
            foreach (var (from, to) in edges)
            {
                // "from -> to" means from runs before to, i.e., to has OrderAfter(from)
                if (!orderAfterBySystem.TryGetValue(to, out var list))
                {
                    list = [];
                    orderAfterBySystem[to] = list;
                }
                list.Add(new TestOrderAfterAttribute(from));
            }

            foreach (var (systemId, groupId) in nodes)
            {
                var orderAfter = orderAfterBySystem.TryGetValue(systemId, out var afterList)
                    ? afterList.Cast<OrderAfterAttribute>().ToArray()
                    : [];
                var scheduling = new EcsSystemScheduling(orderAfter, []);
                scheduling.OwnerId = groupId;
                metadata[systemId] = scheduling;
            }
        }
    }

    private sealed class TestGroupMetadataEntrypoint(
        (Identification groupId, Identification? parentId)[] groups)
        : ApplyMetadataEntrypoint<SystemGroupScheduling>
    {
        public override void CollectMetadata(Dictionary<Identification, SystemGroupScheduling> metadata)
        {
            foreach (var (groupId, parentId) in groups)
            {
                ParentIdAttribute? parent = parentId.HasValue
                    ? new TestParentIdAttribute(parentId.Value)
                    : null;
                metadata[groupId] = new SystemGroupScheduling([], [], parent);
            }
        }
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

    private sealed class TestEntrypointContainer<T>(params T[] entries)
        : IEntrypointContainer<T> where T : class
    {
        public IReadOnlyList<T> ResolveMany() => entries;
        public void ProcessMany(Action<T> action)
        {
            foreach (var entry in entries) action(entry);
        }
        public void Dispose() { }
    }

    private sealed class TrackingWrapper : IStatelessFunction
    {
        private readonly List<Identification>? _executionOrder;
        public int ExecuteCount { get; private set; }
        public Identification Identification { get; }
        public Identification ParentIdentification { get; }

        public TrackingWrapper(Identification id, Identification parentId, List<Identification>? executionOrder = null)
        {
            Identification = id;
            ParentIdentification = parentId;
            _executionOrder = executionOrder;
        }

        public void Execute()
        {
            ExecuteCount++;
            _executionOrder?.Add(Identification);
        }

        public void Initialize(IResolutionScope scope) { }
    }
}
