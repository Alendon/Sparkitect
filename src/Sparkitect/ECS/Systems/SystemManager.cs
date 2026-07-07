using Sparkitect.DI;
using Sparkitect.ECS.Commands;
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

    private Dictionary<Identification, IScheduling>? _systemMetadata;
    private Dictionary<Identification, SystemGroupScheduling>? _groupMetadata;
    private Dictionary<Identification, EcsSystemResourceAccess>? _resourceAccess;

    internal IReadOnlyDictionary<Identification, EcsSystemResourceAccess>? ResourceAccess => _resourceAccess;

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

    public void FetchMetadata()
    {
        var loadedMods = gameStateManager.LoadedMods;

        _systemMetadata = new Dictionary<Identification, IScheduling>();
        using var systemContainer = diService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<IScheduling>>(loadedMods);
        systemContainer.ProcessMany(ep => ep.CollectMetadata(_systemMetadata));

        _groupMetadata = new Dictionary<Identification, SystemGroupScheduling>();
        using var groupContainer = diService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<SystemGroupScheduling>>(loadedMods);
        groupContainer.ProcessMany(ep => ep.CollectMetadata(_groupMetadata));

        _resourceAccess = new Dictionary<Identification, EcsSystemResourceAccess>();
        using var resourceContainer = diService.CreateEntrypointContainer<
            ApplyMetadataEntrypoint<EcsSystemResourceAccess>>(loadedMods);
        resourceContainer.ProcessMany(ep => ep.CollectMetadata(_resourceAccess));
    }

    public SystemTreeNode BuildTree(Identification rootGroupId)
    {
        if (_groupMetadata is null || _systemMetadata is null)
            throw new InvalidOperationException("FetchMetadata must be called before BuildTree.");

        if (!_registeredGroups.Contains(rootGroupId))
            throw new InvalidOperationException($"Group {rootGroupId} is not registered.");

        // Build parent -> children maps
        var groupChildren = new Dictionary<Identification, List<Identification>>();

        // Map group->parent from group metadata
        foreach (var (groupId, groupSched) in _groupMetadata)
        {
            if (!_registeredGroups.Contains(groupId)) continue;
            if (groupSched.ParentGroupId is { } parentId)
            {
                if (!groupChildren.TryGetValue(parentId, out var children))
                {
                    children = new List<Identification>();
                    groupChildren[parentId] = children;
                }
                children.Add(groupId);
            }
        }

        // Map system->parent from system metadata (OwnerId = parent group)
        foreach (var (systemId, scheduling) in _systemMetadata)
        {
            if (!_registeredSystems.Contains(systemId)) continue;
            if (scheduling is EcsSystemScheduling ess)
            {
                var parentId = ess.OwnerId.Resolve();
                if (!groupChildren.TryGetValue(parentId, out var children))
                {
                    children = new List<Identification>();
                    groupChildren[parentId] = children;
                }
                children.Add(systemId);
            }
        }

        // Recursively build tree from root
        return BuildNode(rootGroupId, groupChildren);
    }

    public void ExecuteSystems(IWorld world, FrameTiming frameTiming)
    {
        if (!_worldCache.TryGetValue(world, out var cached))
        {
            cached = BuildWorldCache(world);
            _worldCache[world] = cached;
        }

        cached.FrameTimingHolder.Update(frameTiming);

        var sortedAll = cached.Graph.SortedAll;
        var skipRanges = cached.Graph.GroupSkipRanges;
        var groupIds = cached.Graph.GroupIds;

        int i = 0;
        while (i < sortedAll.Count)
        {
            var id = sortedAll[i];

            if (groupIds.Contains(id))
            {
                // Group gate node: check state in tree
                cached.NodeLookup.TryGetValue(id, out var groupNode);
                if (groupNode is null || groupNode.State != SystemState.Active)
                {
                    // Skip entire subtree
                    if (skipRanges.TryGetValue(i, out var skipTo))
                        i = skipTo;
                    else
                        i++;
                    continue;
                }
                i++;
                continue;
            }

            // System node: check own state in tree
            cached.NodeLookup.TryGetValue(id, out var systemNode);
            if (systemNode is not null && systemNode.State == SystemState.Active)
            {
                if (cached.Wrappers.TryGetValue(id, out var wrapper))
                    wrapper.Execute();
            }

            i++;
        }
    }

    public void NotifyRebuild(IWorld world)
    {
        if (_worldCache.TryGetValue(world, out var cached))
        {
            cached.Provider.CleanupQueries();
            _worldCache.Remove(world);
        }
    }

    public void NotifyDispose(IWorld world)
    {
        if (_worldCache.TryGetValue(world, out var cached))
        {
            cached.Provider.CleanupQueries();
            _worldCache.Remove(world);
        }
    }

    internal bool HasCachedWorld(IWorld world) => _worldCache.ContainsKey(world);

    public ICommandBufferAccessor? GetCommandBufferAccessor(IWorld world)
    {
        return _worldCache.TryGetValue(world, out var cached)
            ? cached.CommandBufferAccessor
            : null;
    }

    internal void InjectMetadata(
        Dictionary<Identification, IScheduling> systems,
        Dictionary<Identification, SystemGroupScheduling> groups,
        Dictionary<Identification, EcsSystemResourceAccess>? resourceAccess = null)
    {
        _systemMetadata = systems;
        _groupMetadata = groups;
        _resourceAccess = resourceAccess;
    }

    internal CachedWorldState BuildWorldCache(IWorld world)
    {
        if (_systemMetadata is null || _groupMetadata is null)
            throw new InvalidOperationException($"{nameof(FetchMetadata)} must be called before BuildWorldCache.");

        var tree = world.GetSystemTree()
            ?? throw new InvalidOperationException("World must have a system tree set before building cache.");

        var nodeLookup = BuildNodeLookup(tree);
        var loadedMods = gameStateManager.LoadedMods;

        // Build graph by walking the tree
        var graphBuilder = new EcsGraphBuilder();
        graphBuilder.BuildFromTree(tree, _systemMetadata, _groupMetadata);
        var graph = graphBuilder.Resolve();

        var wrapperTypes = sfManager.GetRegisteredWrapperTypes();
        var provider = new EcsResolutionProvider(world);
        var commandBufferAccessor = new CommandBufferAccessor(world);
        provider.SetCommandBufferAccessor(commandBufferAccessor);
        var frameTimingHolder = new FrameTimingHolder();
        provider.SetFrameTimingHolder(frameTimingHolder);

        var scope = diService.BuildScope(
            gameStateManager.CurrentCoreContainer,
            provider,
            loadedMods,
            wrapperTypes);

        // Only instantiate wrappers for systems (not groups)
        var wrappers = sfManager.InstantiateWrappers(graph.SortedSystems, scope);

        var wrapperMap = new Dictionary<Identification, IStatelessFunction>();
        foreach (var wrapper in wrappers)
        {
            wrapperMap[wrapper.Identification] = wrapper;
        }

        return new CachedWorldState(graph, wrapperMap, provider, commandBufferAccessor, frameTimingHolder, nodeLookup);
    }

    private SystemTreeNode BuildNode(
        Identification groupId,
        Dictionary<Identification, List<Identification>> groupChildren)
    {
        var node = new SystemTreeNode(groupId, isGroup: true);

        if (groupChildren.TryGetValue(groupId, out var children))
        {
            foreach (var childId in children)
            {
                if (_registeredGroups.Contains(childId))
                {
                    // Child is a group -- recurse
                    node.Children.Add(BuildNode(childId, groupChildren));
                }
                else if (_registeredSystems.Contains(childId))
                {
                    // Child is a system -- leaf node
                    node.Children.Add(new SystemTreeNode(childId, isGroup: false));
                }
            }
        }

        return node;
    }

    private static Dictionary<Identification, SystemTreeNode> BuildNodeLookup(SystemTreeNode root)
    {
        var lookup = new Dictionary<Identification, SystemTreeNode>();
        PopulateLookup(root, lookup);
        return lookup;
    }

    private static void PopulateLookup(SystemTreeNode node, Dictionary<Identification, SystemTreeNode> lookup)
    {
        lookup[node.Id] = node;
        foreach (var child in node.Children)
            PopulateLookup(child, lookup);
    }

    internal sealed class CachedWorldState(
        EcsExecutionGraph graph,
        Dictionary<Identification, IStatelessFunction> wrappers,
        EcsResolutionProvider provider,
        CommandBufferAccessor commandBufferAccessor,
        FrameTimingHolder frameTimingHolder,
        Dictionary<Identification, SystemTreeNode> nodeLookup)
    {
        public EcsExecutionGraph Graph { get; } = graph;
        public Dictionary<Identification, IStatelessFunction> Wrappers { get; } = wrappers;
        public EcsResolutionProvider Provider { get; } = provider;
        public CommandBufferAccessor CommandBufferAccessor { get; } = commandBufferAccessor;
        public FrameTimingHolder FrameTimingHolder { get; } = frameTimingHolder;
        public Dictionary<Identification, SystemTreeNode> NodeLookup { get; } = nodeLookup;
    }
}
