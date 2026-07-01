using System.Diagnostics;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

[PublicAPI]
[RenderGraphRegistry.RegisterRenderGraph("stock")]
public sealed partial class RenderGraph : IRenderGraph, IRenderGraphSetupHandler, ISwapchainHandler, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly IDIService _diService;
    private readonly IGameStateManager _gameStateManager;
    private readonly IRenderGraphManager _renderGraphManager;

    private ISparkitWindow _window = null!;

    private DeclarationLedger _ledger = null!;
    private ResourceTransaction _transaction = null!;
    private FrameInstanceContext _frameContext = null!;
    private CompiledPlan _plan = null!;

    // The single pass for the walking skeleton, run in CompiledPlan order in the frame loop.
    private readonly List<ComputePass> _passes = [];

    // The present target: the finishline-marked clear-image leaf the graph reconciles to PresentSrcKhr.
    private IGraphResource<ImageResource>? _presentTarget;

    private VkCommandPool _commandPool = null!;
    private VkCommandBuffer _commandBuffer = null!;
    private VkFence _inFlightFence = null!;
    private VkSemaphore _acquireSemaphore = null!;
    private VkSemaphore _presentSemaphore = null!;
    private VkQueue _graphicsQueue = null!;
    private bool _disposed;
    private bool _setupComplete;
    private long _lastFrameTimestamp;
    
    public required IImageManager ImageManager { private get; init; }

    /// <summary>
    /// Max frames per second; 0 = uncapped. Paced by a busy-wait in <see cref="RunFrame"/>.
    /// </summary>
    public uint MaxFrameRate { get; set; }

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

    public THandler? GetHandler<THandler>() where THandler : class
    {
        if (typeof(THandler) == typeof(IRenderGraph)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(IRenderGraphSetupHandler)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(ISwapchainHandler)) return (THandler)(object)this;
        return null;
    }

    public void SetSwapchain(VkSwapchain swapchain) => ImageManager.SetSwapchain(swapchain);

    public void Setup(IEnumerable<Identification> passIds, ISparkitWindow window, ICoreContainer graphContainer)
    {
        if (_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.Setup: already invoked. Setup runs exactly once per render graph instance.");

        _window = window;
        var modIdList = _gameStateManager.LoadedMods.ToList();

        var (queueFamily, queue) = ResolveGraphicsQueue(_vulkanContext);

        // Build the declaration ledger: resolve each pass, run its single-Use Setup against the graph's
        // setup context, then reference the finishline so binding has a consumer. Facts resolve through
        // the fact keyed factory built against the per-graph container, so a fact's DI dependencies
        // (e.g. the graph-local IImageManager) are injected from that scope.
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

            computePass.Setup(setupContext);
            _passes.Add(computePass);
        }

        // The RG references the finishline moment as a consumer so an unmarked finishline surfaces
        // UndefinedMoment naming this present reader (rather than silently producing no present binding).
        _transaction.Declare(new FinishlineReaderDescription());

        var linkResult = new GraphCompiler(_ledger).Link();
        if (linkResult is Result<CompiledPlan, CompileError>.Error linkError)
            throw new InvalidOperationException(
                $"RenderGraph.Setup: graph compilation failed: {linkError.Value}.");
        _plan = ((Result<CompiledPlan, CompileError>.Ok)linkResult).Value;

        // Resolve the present target from the graph itself: the finishline moment binds at link to the
        // single increment that marked it, so its published image is the present target. Mint a reference
        // to that bound node and wrap it in a frame-context handle the frame loop re-fetches each frame.
        if (!_plan.ResolvedMoments.TryGetValue(GraphMomentID.Sparkitect.Finishline, out var finishline))
            throw new InvalidOperationException(
                "RenderGraph.Setup: the finishline moment was not published — exactly one pass must mark " +
                "its present-target increment with the finishline moment so the graph can reconcile it to present.");
        var presentRef = _ledger.ReferenceTo<ImageResource>(finishline.IncrementNode);
        _presentTarget = new GraphResourceHandle<ImageResource>(presentRef, _frameContext);

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

        var presResult = _vulkanContext.CreateSemaphore();
        if (presResult is not Result<VkSemaphore, VkApiResult>.Ok presOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (present) failed.");
        _presentSemaphore = presOk.Value;

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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        unsafe { _vulkanContext.VkApi.DeviceWaitIdle(_vulkanContext.VkDevice.Handle); }

        if (_setupComplete)
        {
            _presentSemaphore.Dispose();
            _acquireSemaphore.Dispose();
            _inFlightFence.Dispose();
            _commandPool.Dispose();
        }
    }
}
