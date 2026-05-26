using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Stock render graph. Frame-driving (owns command pool, command buffer, in-flight fence,
/// acquire and present semaphores) but externally invoked — consumers call
/// <see cref="RunFrame"/>. Constructed via <see cref="IRenderGraphManager.CreateGraph{TRenderGraph}"/>;
/// post-construction setup work is driven by <see cref="IRenderGraphSetupHandler.Setup"/>.
/// </summary>
[PublicAPI]
[RenderGraphRegistry.RegisterRenderGraph("stock")]
public sealed partial class RenderGraph : IRenderGraph, IExternalResourceHandler, IRenderGraphSetupHandler, IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly IImageResourceManager _imageManager;
    private readonly IDIService _diService;
    private readonly IGameStateManager _gameStateManager;
    private readonly IRenderGraphManager _renderGraphManager;

    private ISparkitWindow _window = null!;
    private ICoreContainer? _childContainer;

    private CompiledRenderGraph _compiled = null!;
    private VkCommandPool _commandPool = null!;
    private VkCommandBuffer _commandBuffer = null!;
    private VkFence _inFlightFence = null!;
    private VkSemaphore _acquireSemaphore = null!;
    private VkSemaphore _presentSemaphore = null!;
    private VkQueue _graphicsQueue = null!;
    private uint _graphicsQueueFamily;
    private bool _disposed;
    private bool _setupComplete;

    public THandler? GetHandler<THandler>() where THandler : class
    {
        if (typeof(THandler) == typeof(IRenderGraph)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(IExternalResourceHandler)) return (THandler)(object)this;
        if (typeof(THandler) == typeof(IRenderGraphSetupHandler)) return (THandler)(object)this;
        return null;
    }

    public void Publish<TResource>(TResource value)
    {
        if (typeof(TResource) == typeof(SwapchainResource))
        {
            _imageManager.Apply((SwapchainResource)(object)value!);
            return;
        }
        throw new InvalidOperationException(
            $"RenderGraph.Publish: no external-resource route registered for type {typeof(TResource).FullName}.");
    }

    internal RenderGraph(
        IVulkanContext vulkanContext,
        IImageResourceManager imageManager,
        IDIService diService,
        IGameStateManager gameStateManager,
        IRenderGraphManager renderGraphManager)
    {
        _vulkanContext = vulkanContext;
        _imageManager = imageManager;
        _diService = diService;
        _gameStateManager = gameStateManager;
        _renderGraphManager = renderGraphManager;
    }

    /// <summary>
    /// The per-render-graph child container, assigned by the render-graph manager after this
    /// instance is resolved from the SG-emitted keyed factory. Holds the GraphLocal singletons
    /// for this graph; disposed by <see cref="Dispose"/>.
    /// </summary>
    internal ICoreContainer? ChildContainer
    {
        get => _childContainer;
        set => _childContainer = value;
    }

    public void Setup(IEnumerable<Identification> passIds, ISparkitWindow window)
    {
        if (_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.Setup: already invoked. Setup runs exactly once per render graph instance.");

        _window = window;
        var modIdList = _gameStateManager.LoadedMods.ToList();
        var hostContainer = _gameStateManager.CurrentCoreContainer;

        var (queueFamily, queue) = ResolveGraphicsQueue(_vulkanContext);
        _imageManager.BindQueueFamily(queueFamily);

        var managersByType = new Dictionary<Type, IGraphResourceManager>
        {
            [typeof(ImageResourceManager)] = (IGraphResourceManager)_imageManager,
        };
        var setupContext = new SetupContext(_renderGraphManager, managersByType);

        using var passFactory = RenderPassRegistry.BuildRegisterPassContainer(
            _diService, hostContainer, provider: null, modIdList);

        var compiler = new RenderGraphCompiler();
        foreach (var id in passIds)
        {
            if (!_renderGraphManager.RegisteredPassIds.Contains(id))
                throw new InvalidOperationException(
                    $"Pass {id} is not registered via RenderPassRegistry.");
            if (!passFactory.TryResolve(id, out var pass))
                throw new InvalidOperationException(
                    $"No render pass factory resolved {id} — DI binding missing.");
            setupContext.PushPass(id);
            ((ISetupHook)pass).Setup(setupContext);
            setupContext.PopPass();
            compiler.AddPass(id, pass);
        }
        _compiled = compiler.Compile();

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
        _graphicsQueueFamily = queueFamily;
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

        (_imageManager as IDisposable)?.Dispose();
        if (_setupComplete)
        {
            _presentSemaphore.Dispose();
            _acquireSemaphore.Dispose();
            _inFlightFence.Dispose();
            _commandPool.Dispose();
        }

        (_childContainer as IDisposable)?.Dispose();
    }
}
