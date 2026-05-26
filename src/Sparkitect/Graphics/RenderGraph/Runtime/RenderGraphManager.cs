using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Metadata;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Consolidated render-graph manager. Holds the pass/resource/render-graph-type catalogs,
/// the resource-manager-binding map, the per-graph service list metadata, and the
/// graph-local service factory map. Drives construction of render-graph instances via
/// <see cref="CreateGraph{TRenderGraph}"/>.
/// </summary>
[StateService<IRenderGraphManager, RenderGraphModule>]
[PublicAPI]
internal sealed class RenderGraphManager :
    IRenderGraphManager,
    IRenderGraphManagerRegistryFacade,
    IRenderGraphManagerStateFacade
{
    private readonly HashSet<Identification> _passIds = [];
    private readonly HashSet<Identification> _resourceIds = [];
    private readonly HashSet<Identification> _renderGraphIds = [];

    private readonly Dictionary<Identification, Type> _managerByResourceId = [];
    private readonly Dictionary<Identification, RGServiceListMetadata> _serviceListsByGraphId = [];
    private readonly Dictionary<Type, Type> _graphLocalFactories = [];

    public required IDIService DIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

    public IReadOnlyCollection<Identification> RegisteredPassIds => _passIds;
    public IReadOnlyCollection<Identification> RegisteredResourceIds => _resourceIds;
    public IReadOnlyCollection<Identification> RegisteredRenderGraphIds => _renderGraphIds;

    public Type GetManagerTypeFor(Identification resourceId) =>
        _managerByResourceId.TryGetValue(resourceId, out var t)
            ? t
            : throw new KeyNotFoundException(
                $"No resource manager registered for resource id {resourceId}.");

    public bool TryGetManagerType(Identification resourceId, out Type managerType) =>
        _managerByResourceId.TryGetValue(resourceId, out managerType!);

    public bool TryGetRenderGraphType(Identification renderGraphId, out Type renderGraphType)
    {
        renderGraphType = null!;
        return _renderGraphIds.Contains(renderGraphId)
            && _serviceListsByGraphId.ContainsKey(renderGraphId);
    }

    public void AddPass(Identification id) => _passIds.Add(id);
    public void AddResource(Identification id) => _resourceIds.Add(id);
    public void AddRenderGraphType(Identification id) => _renderGraphIds.Add(id);

    public void PostRegistry()
    {
        var modIdList = GameStateManager.LoadedMods.ToList();

        var bindings = new Dictionary<Identification, ResourceManagerBinding>();
        using (var container = DIService.CreateEntrypointContainer<
                   ApplyMetadataEntrypoint<ResourceManagerBinding>>(modIdList))
        {
            container.ProcessMany(ep => ep.CollectMetadata(bindings));
        }
        _managerByResourceId.Clear();
        foreach (var id in _resourceIds)
        {
            if (!bindings.TryGetValue(id, out var binding))
                throw new InvalidOperationException(
                    $"Resource id {id} is tracked via GraphResourceRegistry " +
                    "but no [ResourceManager<T>] metadata was found.");
            _managerByResourceId[id] = binding.ManagerType;
        }

        var serviceLists = new Dictionary<Identification, RGServiceListMetadata>();
        using (var container = DIService.CreateEntrypointContainer<
                   ApplyMetadataEntrypoint<RGServiceListMetadata>>(modIdList))
        {
            container.ProcessMany(ep => ep.CollectMetadata(serviceLists));
        }
        _serviceListsByGraphId.Clear();
        foreach (var kvp in serviceLists)
            _serviceListsByGraphId[kvp.Key] = kvp.Value;

        _graphLocalFactories.Clear();
        using (var container = DIService.CreateEntrypointContainer<IGraphLocalServiceEntry>(modIdList))
        {
            container.ProcessMany(entry =>
                _graphLocalFactories[entry.ServiceInterface] = entry.FactoryType);
        }
    }

    public TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification
    {
        var rgId = TRenderGraph.Identification;

        if (!_serviceListsByGraphId.TryGetValue(rgId, out var serviceList))
            throw new InvalidOperationException(
                $"No RGServiceListMetadata registered for render graph {rgId}. " +
                "Ship an ApplyMetadataEntrypoint<RGServiceListMetadata> subclass for this graph type.");

        var hostContainer = GameStateManager.CurrentCoreContainer;
        var childBuilder = DIService.CreateChildContainerBuilder(hostContainer);
        foreach (var iface in serviceList.ServiceInterfaces)
        {
            if (!_graphLocalFactories.TryGetValue(iface, out var factoryType))
                throw new InvalidOperationException(
                    $"No [GraphLocal<{iface.Name}>] factory registered for service interface " +
                    $"{iface.FullName} required by render graph {rgId}.");
            childBuilder.Register(factoryType);
        }
        var childContainer = childBuilder.Build();

        var passIdList = passIds.ToList();
        var modIdList = GameStateManager.LoadedMods.ToList();

        using var rgFactory = RenderGraphRegistry.BuildRegisterRenderGraphContainer(
            DIService,
            childContainer,
            provider: null,
            modIdList);

        if (!rgFactory.TryResolve(rgId, out var rg))
            throw new InvalidOperationException(
                $"No render-graph factory resolved for {rgId} — registration missing or DI deps unmet.");

        if (rg is RenderGraph stockRg)
            stockRg.ChildContainer = childContainer;

        var setupHandler = rg.GetHandler<IRenderGraphSetupHandler>()
            ?? throw new InvalidOperationException(
                $"Render graph {rgId} does not expose IRenderGraphSetupHandler.");
        setupHandler.Setup(passIdList, window);
        
        var swapchainResource = new SwapchainResource(window.Swapchain);
        swapchainResource.Apply(rg);

        return (TRenderGraph)rg;
    }
}
