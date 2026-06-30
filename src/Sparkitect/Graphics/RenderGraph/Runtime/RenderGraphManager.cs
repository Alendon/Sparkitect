using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Thin, GameState-owned render-graph manager. Holds the pass, render-graph-type, and resource-moment
/// catalogs and drives construction of render-graph instances via <see cref="CreateGraph{TRenderGraph}"/>.
/// The graph is resolved directly through the registry's generated keyed factory against the host
/// container — no child container, no per-graph service-list metadata, no graph-local service map,
/// and no resource-manager bindings. The resource-moment catalog is the demoted moment store: a simple
/// collection owned here, not a separate service.
/// </summary>
[StateService<IRenderGraphManager, RenderGraphModule>]
[PublicAPI]
internal sealed class RenderGraphManager :
    IRenderGraphManager,
    IRenderGraphManagerRegistryFacade
{
    private readonly HashSet<Identification> _passIds = [];
    private readonly HashSet<Identification> _renderGraphIds = [];
    private readonly Dictionary<Identification, ResourceMomentDefinition> _resourceMoments = [];

    public required IDIService DIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

    public IReadOnlyCollection<Identification> RegisteredPassIds => _passIds;
    public IReadOnlyCollection<Identification> RegisteredRenderGraphIds => _renderGraphIds;
    public IReadOnlyDictionary<Identification, ResourceMomentDefinition> RegisteredResourceMoments => _resourceMoments;

    public void AddPass(Identification id) => _passIds.Add(id);
    public void AddRenderGraphType(Identification id) => _renderGraphIds.Add(id);
    public void AddResourceMoment(Identification id, ResourceMomentDefinition definition) =>
        _resourceMoments[id] = definition;

    public void AddFact<TFact>() where TFact : IHasIdentification
    {
        throw new NotImplementedException();
    }

    public TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification
    {
        var rgId = TRenderGraph.Identification;

        var passIdList = passIds.ToList();
        var hostContainer = GameStateManager.CurrentCoreContainer;
        var modIdList = GameStateManager.LoadedMods.ToList();

        using var rgFactory = RenderGraphRegistry.BuildRegisterRenderGraphContainer(
            DIService,
            hostContainer,
            provider: null,
            modIdList);

        if (!rgFactory.TryResolve(rgId, out var rg))
            throw new InvalidOperationException(
                $"No render-graph factory resolved for {rgId} — registration missing or DI deps unmet.");

        var setupHandler = rg.GetHandler<IRenderGraphSetupHandler>()
            ?? throw new InvalidOperationException(
                $"Render graph {rgId} does not expose IRenderGraphSetupHandler.");
        setupHandler.Setup(passIdList, window);

        var swapchainHandler = rg.GetHandler<ISwapchainHandler>()
            ?? throw new InvalidOperationException(
                $"Render graph {rgId} does not expose ISwapchainHandler.");
        swapchainHandler.SetSwapchain(window.Swapchain);

        return (TRenderGraph)rg;
    }
}
