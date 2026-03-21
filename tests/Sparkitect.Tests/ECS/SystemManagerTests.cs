using Imposter.Abstractions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
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

    private static readonly Identification System1 = Identification.Create(1, 2, 1);
    private static readonly Identification System2 = Identification.Create(1, 2, 2);
    private static readonly Identification System3 = Identification.Create(1, 2, 3);

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

        world.AddSystem(System1);
        world.AddSystem(System2);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);

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

        world.AddSystem(System1);
        world.AddSystem(System2);
        world.SetSystemState(System2, SystemState.Inactive);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);

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
            [wrapper1, wrapper2]);
        var world = IWorld.Create();

        world.AddSystem(System1);
        world.AddSystem(System2);
        world.AddSystemGroup(GroupA);
        world.AddSystemGroup(GroupB);
        world.SetGroupState(GroupB, SystemState.Inactive);

        manager.ExecuteSystems(world);

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

        world.AddSystem(System1);
        world.AddSystem(System2);
        world.AddSystem(System3);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);

        await Assert.That(executionOrder).HasCount().EqualTo(3);
        await Assert.That(executionOrder[0]).IsEqualTo(System3);
        await Assert.That(executionOrder[1]).IsEqualTo(System1);
        await Assert.That(executionOrder[2]).IsEqualTo(System2);
    }

    // --- Cache Tests ---

    [Test]
    public async Task NotifyRebuild_ClearsCachedGraphForWorld()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        world.AddSystem(System1);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);
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
        world.AddSystem(System1);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);
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

        world1.AddSystem(System1);
        world1.AddSystemGroup(GroupA);

        world2.AddSystem(System2);
        world2.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world1);
        manager.ExecuteSystems(world2);

        // Both wrappers available in cache, but only active systems in each world execute
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
        world.AddSystem(System1);
        world.AddSystemGroup(GroupA);

        await Assert.That(manager.HasCachedWorld(world)).IsFalse();
        manager.ExecuteSystems(world);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    [Test]
    public async Task ExecuteSystems_UsesCachedStateOnSubsequentCalls()
    {
        var wrapper = new TrackingWrapper(System1, GroupA);
        var manager = CreateManager([(System1, GroupA)], [wrapper]);
        var world = IWorld.Create();
        world.AddSystem(System1);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);
        manager.ExecuteSystems(world);

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
        world.AddSystem(System1);
        world.AddSystemGroup(GroupA);

        manager.ExecuteSystems(world);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();

        manager.NotifyRebuild(world);
        await Assert.That(manager.HasCachedWorld(world)).IsFalse();

        manager.ExecuteSystems(world);
        await Assert.That(manager.HasCachedWorld(world)).IsTrue();
    }

    // --- EcsResolutionProvider Tests ---

    [Test]
    public async Task EcsResolutionProvider_TryResolve_ReturnsFalse()
    {
        var provider = new EcsResolutionProvider();

        var result = provider.TryResolve(typeof(string), null!, [], out var service);

        await Assert.That(result).IsFalse();
        await Assert.That(service).IsNull();
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
        (Identification from, Identification to)[]? edges = null)
    {
        var sfManagerImposter = new IStatelessFunctionManagerImposter(ImposterMode.Implicit);
        var diServiceImposter = new IDIServiceImposter(ImposterMode.Implicit);
        var gsmImposter = new IGameStateManagerImposter(ImposterMode.Implicit);
        var coreContainerImposter = new ICoreContainerImposter(ImposterMode.Implicit);
        var scopeImposter = new IResolutionScopeImposter(ImposterMode.Implicit);

        // Configure IGameStateManager
        gsmImposter.LoadedMods.Getter().Returns(Array.Empty<string>());
        gsmImposter.CurrentCoreContainer.Getter().Returns(coreContainerImposter.Instance());

        // Configure IDIService.CreateEntrypointContainer to return a test container
        // that populates the graph with the specified nodes
        var entrypoint = new TestSchedulingEntrypoint(graphNodes, edges ?? []);
        var container = new TestEntrypointContainer<
            ApplySchedulingEntrypoint<EcsSystemFunctionAttribute, EcsSystemContext, IEcsGraphBuilder>>(entrypoint);
        diServiceImposter.CreateEntrypointContainer<
                ApplySchedulingEntrypoint<EcsSystemFunctionAttribute, EcsSystemContext, IEcsGraphBuilder>>(
                Arg<IEnumerable<string>>.Any())
            .Returns(container);

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

        return new SystemManager(
            sfManagerImposter.Instance(),
            diServiceImposter.Instance(),
            gsmImposter.Instance());
    }

    private sealed class TestSchedulingEntrypoint(
        (Identification systemId, Identification groupId)[] nodes,
        (Identification from, Identification to)[] edges)
        : ApplySchedulingEntrypoint<EcsSystemFunctionAttribute, EcsSystemContext, IEcsGraphBuilder>
    {
        public override void BuildGraph(IEcsGraphBuilder builder, EcsSystemContext context)
        {
            foreach (var (systemId, groupId) in nodes)
                builder.AddNode(systemId, groupId);
            foreach (var (from, to) in edges)
                builder.AddEdge(from, to, optional: false);
        }
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
