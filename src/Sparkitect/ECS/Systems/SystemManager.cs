using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Metadata;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[StateService<ISystemManager, EcsModule>]
internal class SystemManager(
    IStatelessFunctionManager sfManager,
    IDIService diService,
    IGameStateManager gameStateManager) : ISystemManager
{
    private readonly HashSet<Identification> _registeredSystems = [];
    private readonly HashSet<Identification> _registeredGroups = [];
    private readonly Dictionary<IWorld, CachedWorldState> _worldCache = new();

    internal IReadOnlySet<Identification> RegisteredSystems => _registeredSystems;
    internal IReadOnlySet<Identification> RegisteredGroups => _registeredGroups;

    public void RegisterSystem(Identification id)
    {
        _registeredSystems.Add(id);
    }

    public void RegisterSystemGroup(Identification id)
    {
        _registeredGroups.Add(id);
    }

    public void ExecuteSystems(IWorld world)
    {
        if (!_worldCache.TryGetValue(world, out var cached))
        {
            cached = BuildWorldCache(world);
            _worldCache[world] = cached;
        }

        var systems = world.GetSystems();
        var groups = world.GetSystemGroups();

        foreach (var systemId in cached.Graph.SortedSystems)
        {
            // Skip inactive systems
            if (!systems.TryGetValue(systemId, out var systemState) || systemState != SystemState.Active)
                continue;

            // Skip systems whose group is inactive
            if (cached.Graph.GroupMembership.TryGetValue(systemId, out var groupId)
                && groups.TryGetValue(groupId, out var groupState)
                && groupState != SystemState.Active)
                continue;

            if (cached.Wrappers.TryGetValue(systemId, out var wrapper))
                wrapper.Execute();
        }
    }

    public void NotifyRebuild(IWorld world)
    {
        _worldCache.Remove(world);
    }

    public void NotifyDispose(IWorld world)
    {
        _worldCache.Remove(world);
    }

    internal bool HasCachedWorld(IWorld world) => _worldCache.ContainsKey(world);

    internal CachedWorldState BuildWorldCache(IWorld world)
    {
        var context = new EcsSystemContext { World = world };
        var loadedMods = gameStateManager.LoadedMods;

        // Collect ECS system scheduling metadata
        var metadata = new Dictionary<Identification, IScheduling>();
        using var entrypointContainer = diService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<IScheduling>>(loadedMods);
        entrypointContainer.ProcessMany(ep => ep.CollectMetadata(metadata));

        // Build ECS graph from metadata
        var graphBuilder = new EcsGraphBuilder();
        foreach (var (id, scheduling) in metadata)
        {
            if (scheduling is EcsSystemScheduling ess)
                ess.BuildGraph(graphBuilder, context, id);
        }

        var graph = graphBuilder.Resolve();

        var wrapperTypes = sfManager.GetRegisteredWrapperTypes();
        var provider = new EcsResolutionProvider();
        var scope = diService.BuildScope(
            gameStateManager.CurrentCoreContainer,
            provider,
            loadedMods,
            wrapperTypes);

        var wrappers = sfManager.InstantiateWrappers(graph.SortedSystems, scope);

        var wrapperMap = new Dictionary<Identification, IStatelessFunction>();
        foreach (var wrapper in wrappers)
        {
            wrapperMap[wrapper.Identification] = wrapper;
        }

        return new CachedWorldState(graph, wrapperMap);
    }

    internal sealed class CachedWorldState(
        EcsExecutionGraph graph,
        Dictionary<Identification, IStatelessFunction> wrappers)
    {
        public EcsExecutionGraph Graph { get; } = graph;
        public Dictionary<Identification, IStatelessFunction> Wrappers { get; } = wrappers;
    }
}
