using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Thin, GameState-owned render-graph manager. Holds the pass, fact, render-graph-type, and
/// resource-moment catalogs and drives construction of render-graph instances via
/// <see cref="CreateGraph{TRenderGraph}"/>. Each graph is resolved through the registry's generated
/// keyed factory against a per-graph child container — layered over the host container and populated
/// with the <c>[GraphLocal&lt;,IRenderGraph&gt;]</c> services — so graph-local managers (e.g. the image
/// manager) and facts resolve their dependencies from that scope. The resource-moment catalog is the
/// demoted moment store: a simple collection owned here, not a separate service.
/// </summary>
[StateService<IRenderGraphManager, RenderGraphModule>]
[PublicAPI]
internal sealed class RenderGraphManager :
    IRenderGraphManager,
    IRenderGraphManagerRegistryFacade
{
    private readonly HashSet<Identification> _passIds = [];
    private readonly HashSet<Identification> _factIds = [];
    private readonly HashSet<Identification> _renderGraphIds = [];
    private readonly Dictionary<Identification, ResourceMomentDefinition> _resourceMoments = [];

    public required IDIService DIService { private get; init; }
    public required IGameStateManager GameStateManager { private get; init; }

    public IReadOnlyCollection<Identification> RegisteredPassIds => _passIds;
    public IReadOnlyCollection<Identification> RegisteredFactIds => _factIds;
    public IReadOnlyCollection<Identification> RegisteredRenderGraphIds => _renderGraphIds;
    public IReadOnlyDictionary<Identification, ResourceMomentDefinition> RegisteredResourceMoments => _resourceMoments;

    public void AddPass(Identification id) => _passIds.Add(id);
    public void AddRenderGraphType(Identification id) => _renderGraphIds.Add(id);
    public void AddResourceMoment(Identification id, ResourceMomentDefinition definition) =>
        _resourceMoments[id] = definition;

    public void AddFact<TFact>() where TFact : IHasIdentification => _factIds.Add(TFact.Identification);

    public TRenderGraph CreateGraph<TRenderGraph>(IEnumerable<Identification> passIds, ISparkitWindow window)
        where TRenderGraph : class, IRenderGraph, IHasIdentification
    {
        var rgId = TRenderGraph.Identification;

        var passIdList = passIds.ToList();
        var hostContainer = GameStateManager.CurrentCoreContainer;
        var modIdList = GameStateManager.LoadedMods.ToList();

        // Per-render-graph core container: collects [GraphLocal<,IRenderGraph>] configurator
        // entrypoints layered over the host container. With no graph-local services registered it
        // resolves identically to the host container via parent fallback.
        var graphContainer = DIService.BuildConfiguredContainer<IGraphLocalConfigurator>(
            hostContainer,
            modIdList,
            typeof(GraphLocalServiceEntryAttribute<>).MakeGenericType(typeof(IRenderGraph)),
            (configurator, builder, loadedMods) => configurator.Configure(builder, loadedMods));

        using var rgFactory = RenderGraphRegistry.BuildRegisterRenderGraphContainer(
            DIService,
            graphContainer,
            provider: null,
            modIdList);

        if (!rgFactory.TryResolve(rgId, out var rg))
            throw new InvalidOperationException(
                $"No render-graph factory resolved for {rgId} — registration missing or DI deps unmet.");

        var setupHandler = rg.GetHandler<IRenderGraphSetupHandler>()
            ?? throw new InvalidOperationException(
                $"Render graph {rgId} does not expose IRenderGraphSetupHandler.");
        setupHandler.Setup(passIdList, window, graphContainer);

        var swapchainHandler = rg.GetHandler<ISwapchainHandler>()
            ?? throw new InvalidOperationException(
                $"Render graph {rgId} does not expose ISwapchainHandler.");
        swapchainHandler.SetSwapchain(window.Swapchain);

        return (TRenderGraph)rg;
    }
}
