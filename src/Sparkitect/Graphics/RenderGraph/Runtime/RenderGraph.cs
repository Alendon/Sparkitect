using System.Diagnostics;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Push;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Metadata;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Settings;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>The stock render graph: the L2 GPU/Vulkan specialization that runs registered passes, drives per-frame present, and exposes its capabilities as typed handlers. Registered as <c>"stock"</c> and resolved through <see cref="IRenderGraph"/>.</summary>
[PublicAPI]
[RenderGraphRegistry.RegisterRenderGraph("stock")]
public sealed partial class RenderGraph : IRenderGraph, IRenderGraphSetupHandler, ISwapchainHandler,
    IExternalPushHandler, IDisposable, IHasIdentification
{
    private readonly IVulkanContext _vulkanContext;
    private readonly IDIService _diService;
    private readonly IGameStateManager _gameStateManager;
    private readonly IRenderGraphManager _renderGraphManager;

    // The FPS cap is read inline from this at frame pacing (D-17). Resolved from the per-graph container
    // (which falls back to the CoreModule host) at Setup, before any frame runs.
    private ISettingsManager _settingsManager = null!;

    private ISparkitWindow _window = null!;

    private DeclarationLedger _ledger = null!;
    private ResourceTransaction _transaction = null!;
    private FrameInstanceContext _frameContext = null!;
    private CompiledPlan _plan = null!;

    // The per-graph container owning the graph-local service singletons; disposed at teardown so its
    // IDisposable services (e.g. the descriptor-layout cache) release their GPU objects.
    private ICoreContainer _graphContainer = null!;

    // _passRoots[i] holds pass i's root resources (parallel to _passes).
    private readonly List<ComputePass> _passes = [];
    private readonly List<IReadOnlyList<RootResource>> _passRoots = [];

    // Graph-owned swap-copy store for externally-pushed snapshots, keyed by moment; the publish door
    // delegates here and the frame-start step reads each moment's latest snapshot from it.
    private readonly PushStore _pushStore = new();

    // The registered externally-pushed moments this graph synthesized a chain head for at Setup; the
    // frame-start step rebinds each one's latest snapshot before the pass loop.
    private readonly List<Identification> _pushedMoments = [];

    // The finishline-marked present-target leaf; the graph asserts its carried state is PresentSrcKhr.
    private IGraphResource<ImageResource>? _presentTarget;

    // The root that published the finishline moment; the frame loop dispatches the present hook on it after all passes.
    private RootResource? _finishlinePublisher;
    private bool _presentBackingValidated;

    private VkCommandPool _commandPool = null!;
    private VkCommandBuffer _commandBuffer = null!;
    private VkFence _inFlightFence = null!;
    private VkSemaphore _acquireSemaphore = null!;
    // One present semaphore per swapchain image: a binary semaphore signaled for present must not be
    // re-signaled until its prior present consumed it, which the in-flight fence does not guarantee across images.
    private VkSemaphore[] _presentSemaphores = null!;
    private VkQueue _graphicsQueue = null!;
    private bool _disposed;
    private bool _setupComplete;
    private long _lastFrameTimestamp;
    
    /// <summary>Graph-local image backing provider, injected from the per-graph container.</summary>
    public required IImageManager ImageManager { private get; init; }

    /// <summary>Graph-local storage-buffer backing provider, injected from the per-graph container alongside <see cref="ImageManager"/>.</summary>
    public required IBufferManager BufferManager { private get; init; }

    internal RenderGraph(
        IVulkanContext vulkanContext,
        IDIService diService,
        IGameStateManager gameStateManager,
        IRenderGraphManager renderGraphManager)
    {
        _vulkanContext = vulkanContext;
        _diService = diService;
        _gameStateManager = gameStateManager;
        _renderGraphManager = renderGraphManager;
    }

    /// <inheritdoc/>
    public THandler? GetHandler<THandler>() where THandler : class
    {
        if (typeof(THandler) == typeof(IRenderGraph)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(IRenderGraphSetupHandler)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(ISwapchainHandler)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(IExternalPushHandler)) return (THandler)(object)this;
        return null;
    }

    /// <inheritdoc/>
    public void SetSwapchain(VkSwapchain swapchain) => ImageManager.SetSwapchain(swapchain);

    /// <inheritdoc/>
    public void Publish<T>(Identification moment, ReadOnlySpan<T> data) where T : unmanaged =>
        _pushStore.Publish(moment, data);

    /// <summary>Builds the graph once: runs each pass's Setup, compiles the declarations into an order, and wires the per-graph container. Throws if invoked a second time.</summary>
    public void Setup(IEnumerable<Identification> passIds, ISparkitWindow window, ICoreContainer graphContainer)
    {
        if (_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.Setup: already invoked. Setup runs exactly once per render graph instance.");

        _window = window;
        _graphContainer = graphContainer;
        // The per-graph container falls back to the CoreModule host, so the CoreModule settings manager
        // resolves here; the pacing check reads the live fps_cap through it each frame.
        _settingsManager = graphContainer.Resolve<ISettingsManager>();
        var modIdList = _gameStateManager.LoadedMods.ToList();

        var (queueFamily, queue) = ResolveGraphicsQueue(_vulkanContext);

        // Facts resolve through the fact keyed factory built against the per-graph container, so a fact's
        // DI dependencies (e.g. the graph-local IImageManager) are injected from that scope.
        _ledger = new DeclarationLedger();
        using var factFactory = FactRegistry.BuildRegisterContainer(
            _diService, graphContainer, provider: null, modIdList);
        _transaction = new ResourceTransaction(_ledger, factFactory);
        _frameContext = new FrameInstanceContext();
        var setupContext = new GraphSetupContext(_transaction, _frameContext);

        using var passFactory = RenderPassRegistry.BuildRegisterPassContainer(
            _diService, graphContainer, provider: null, modIdList);

        foreach (var id in passIds)
        {
            if (!_renderGraphManager.RegisteredPassIds.Contains(id))
                throw new InvalidOperationException(
                    $"Pass {id} is not registered via RenderPassRegistry.");
            if (!passFactory.TryResolve(id, out var pass))
                throw new InvalidOperationException(
                    $"No render pass factory resolved {id} — DI binding missing.");
            if (pass is not ComputePass computePass)
                throw new InvalidOperationException(
                    $"Render pass {id} ({pass.GetType().FullName}) is not a ComputePass — the walking skeleton " +
                    "only supports compute-category passes.");

            // Bracket the pass's Setup so the context records the root resource handles it declares.
            setupContext.BeginPass();
            computePass.Setup(setupContext);
            _passRoots.Add(setupContext.EndPass());
            _passes.Add(computePass);
        }

        // For each registered externally-pushed moment, synthesize a marked birth increment (a pushed-leaf
        // chain head) so GraphCompiler.BindMoments binds it and its readers link with no pass authoring the
        // mark — mirroring the finishline consumer declaration below. Config is Identification-mapped
        // metadata read through the general metadata mechanism.
        var pushConfigs = new Dictionary<Identification, ExternalPushConfig>();
        using (var pushContainer = _diService.CreateEntrypointContainer<
                   ApplyMetadataEntrypoint<ExternalPushConfig>>(modIdList))
        {
            pushContainer.ProcessMany(ep => ep.CollectMetadata(pushConfigs));
        }

        foreach (var (momentId, _) in _renderGraphManager.RegisteredResourceMoments)
        {
            if (!pushConfigs.ContainsKey(momentId))
                continue;
            _transaction.Declare(new PushedLeafDescription(momentId));
            _pushedMoments.Add(momentId);
        }

        // Reference the finishline moment as a consumer so an unmarked finishline surfaces UndefinedMoment.
        _transaction.Declare(new FinishlineReaderDescription());

        var linkResult = new GraphCompiler(_ledger).Link();
        if (linkResult is Result<CompiledPlan, CompileError>.Error linkError)
            throw new InvalidOperationException(
                $"RenderGraph.Setup: graph compilation failed: {linkError.Value}.");
        _plan = ((Result<CompiledPlan, CompileError>.Ok)linkResult).Value;

        // The finishline moment binds at link to the single increment that marked it; its published image
        // is the present target, wrapped in a frame-context handle the frame loop re-fetches each frame.
        if (!_plan.ResolvedMoments.TryGetValue(GraphMomentID.Sparkitect.Finishline, out var finishline))
            throw new InvalidOperationException(
                "RenderGraph.Setup: the finishline moment was not published — exactly one pass must mark " +
                "its present-target increment with the finishline moment so the graph can reconcile it to present.");
        var presentRef = _ledger.ReferenceTo<ImageResource>(finishline.IncrementNode);
        _presentTarget = new GraphResourceHandle<ImageResource>(presentRef, _frameContext);

        // Correlate the finishline-publishing root: the marked increment lives on the same chain the
        // publishing description declared, so match the recorded pass root whose chain id equals it.
        _finishlinePublisher = FindRootByChain(ResolveChain(finishline.IncrementNode));

        var poolResult = _vulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit, queueFamily);
        if (poolResult is not Result<VkCommandPool, VkApiResult>.Ok poolOk)
            throw new InvalidOperationException("RenderGraph: CreateCommandPool failed.");
        _commandPool = poolOk.Value;

        var cmdResult = _commandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (cmdResult is not Result<VkCommandBuffer, VkApiResult>.Ok cmdOk)
            throw new InvalidOperationException("RenderGraph: AllocateCommandBuffer failed.");
        _commandBuffer = cmdOk.Value;

        var fenceResult = _vulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
        if (fenceResult is not Result<VkFence, VkApiResult>.Ok fenceOk)
            throw new InvalidOperationException("RenderGraph: CreateFence failed.");
        _inFlightFence = fenceOk.Value;

        var acqResult = _vulkanContext.CreateSemaphore();
        if (acqResult is not Result<VkSemaphore, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (acquire) failed.");
        _acquireSemaphore = acqOk.Value;

        var imageCount = _window.Swapchain.Images.Length;
        _presentSemaphores = new VkSemaphore[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            var presResult = _vulkanContext.CreateSemaphore();
            if (presResult is not Result<VkSemaphore, VkApiResult>.Ok presOk)
                throw new InvalidOperationException("RenderGraph: CreateSemaphore (present) failed.");
            _presentSemaphores[i] = presOk.Value;
        }

        _graphicsQueue = queue;
        _lastFrameTimestamp = Stopwatch.GetTimestamp();
        _setupComplete = true;
    }

    private static (uint family, VkQueue queue) ResolveGraphicsQueue(IVulkanContext ctx)
    {
        var props = ctx.VkPhysicalDevice.GetQueueFamilyProperties();
        for (uint i = 0; i < props.Length; i++)
        {
            if ((props[i].QueueFlags & QueueFlags.GraphicsBit) != 0)
            {
                var q = ctx.GetQueue(i, 0)
                    ?? throw new InvalidOperationException($"RenderGraph: GetQueue({i}, 0) returned null.");
                return (i, q);
            }
        }
        throw new InvalidOperationException("RenderGraph: no graphics-capable queue family found on physical device.");
    }

    // The chain (declaring resource node) a ledger node belongs to; None when unknown.
    private GraphNodeId ResolveChain(GraphNodeId nodeId)
    {
        foreach (var node in _ledger.Nodes)
            if (node.Id == nodeId)
                return node.Resource;
        return GraphNodeId.None;
    }

    // The recorded pass root that owns the given chain. Tries a direct match first, then walks composite
    // ownership via TryGetOwningChain. Returns null when no root matches and no further owner exists.
    private RootResource? FindRootByChain(GraphNodeId chain)
    {
        var current = chain;
        while (!current.IsNone)
        {
            foreach (var passRoots in _passRoots)
                foreach (var root in passRoots)
                    if (root.ResourceChain == current)
                        return root;

            if (!_transaction.TryGetOwningChain(current, out var owner))
                return null;
            current = owner;
        }
        return null;
    }

    /// <summary>Waits for the device to idle, then tears down the graph's owned GPU objects. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        unsafe { _vulkanContext.VkApi.DeviceWaitIdle(_vulkanContext.VkDevice.Handle); }

        if (_setupComplete)
        {
            // GPU is idle: dispose the last frame's per-frame instances (Dispose-strategy views) and free
            // the manager-owned transient backing (Release-strategy), before any device object is destroyed.
            _frameContext.Dispose();
            ImageManager.DisposeTransient();
            BufferManager.DisposeBuffers();

            // Passes destroy their owned pipelines/layouts; the graph container disposes the graph-local
            // service singletons (ImageManager is not IDisposable, so DisposeTransient above is not doubled).
            foreach (var pass in _passes)
                pass.Dispose();
            _graphContainer.Dispose();

            foreach (var presentSemaphore in _presentSemaphores)
                presentSemaphore.Dispose();
            _acquireSemaphore.Dispose();
            _inFlightFence.Dispose();
            _commandPool.Dispose();
        }
    }
}
