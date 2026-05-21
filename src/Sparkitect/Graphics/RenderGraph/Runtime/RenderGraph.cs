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
/// <see cref="RunFrame"/>. Constructed via the static <see cref="Initialize"/> factory;
/// no public constructor.
/// </summary>
[PublicAPI]
public sealed partial class RenderGraph : IDisposable
{
    private readonly IVulkanContext _vulkanContext;
    private readonly ISparkitWindow _window;
    private readonly CompiledRenderGraph _compiled;
    private readonly VkCommandPool _commandPool;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkFence _inFlightFence;
    private readonly VkSemaphore _acquireSemaphore;
    private readonly VkSemaphore _presentSemaphore;
    private readonly VkQueue _graphicsQueue;
    private readonly uint _graphicsQueueFamily;
    private readonly IImageResourceManager _imageManager;
    private bool _disposed;

    private RenderGraph(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        CompiledRenderGraph compiled,
        VkCommandPool commandPool,
        VkCommandBuffer commandBuffer,
        VkFence inFlightFence,
        VkSemaphore acquireSemaphore,
        VkSemaphore presentSemaphore,
        VkQueue graphicsQueue,
        uint graphicsQueueFamily,
        IImageResourceManager imageManager)
    {
        _vulkanContext = vulkanContext;
        _window = window;
        _compiled = compiled;
        _commandPool = commandPool;
        _commandBuffer = commandBuffer;
        _inFlightFence = inFlightFence;
        _acquireSemaphore = acquireSemaphore;
        _presentSemaphore = presentSemaphore;
        _graphicsQueue = graphicsQueue;
        _graphicsQueueFamily = graphicsQueueFamily;
        _imageManager = imageManager;
    }

    /// <summary>
    /// Builds a fully initialized, ready-to-<see cref="RunFrame"/> render graph.
    /// </summary>
    public static RenderGraph Initialize(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        IDIService diService,
        ICoreContainer hostContainer,
        IGameStateManager gameStateManager,
        IGraphResourceTypes resourceTypes,
        IPassTypes passTypes,
        IReadOnlyList<Identification> passIds)
    {
        var resolutionProvider = new RenderGraphResolutionProvider();

        var modIdList = gameStateManager.LoadedMods.ToList();
        var passFactory = RenderPassRegistry.BuildRegisterPassContainer(
            diService,
            hostContainer,
            resolutionProvider,
            modIdList);

        var (queueFamily, queue) = ResolveGraphicsQueue(vulkanContext);

        var swapchainBackings = window.Swapchain.Images.ToArray();
        var swapchainImage = new Resources.Image(
            swapchainBackings,
            window.Swapchain.Extent,
            window.Swapchain.ImageFormat,
            initialQueueFamily: queueFamily);

        var imageMgr = new ImageResourceManager(swapchainImage, vulkanContext);
        var managersByType = new Dictionary<Type, IGraphResourceManager>
        {
            [typeof(ImageResourceManager)] = imageMgr,
        };
        var setupContext = new SetupContext(resourceTypes, managersByType);

        var compiler = new RenderGraphCompiler();
        foreach (var id in passIds)
        {
            if (!passTypes.RegisteredPassIds.Contains(id))
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
        var compiled = compiler.Compile();

        var poolResult = vulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit, queueFamily);
        if (poolResult is not Result<VkCommandPool, VkApiResult>.Ok poolOk)
            throw new InvalidOperationException("RenderGraph: CreateCommandPool failed.");
        var pool = poolOk.Value;

        var cmdResult = pool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (cmdResult is not Result<VkCommandBuffer, VkApiResult>.Ok cmdOk)
            throw new InvalidOperationException("RenderGraph: AllocateCommandBuffer failed.");
        var cmdBuf = cmdOk.Value;

        var fenceResult = vulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
        if (fenceResult is not Result<VkFence, VkApiResult>.Ok fenceOk)
            throw new InvalidOperationException("RenderGraph: CreateFence failed.");
        var fence = fenceOk.Value;

        var acqResult = vulkanContext.CreateSemaphore();
        if (acqResult is not Result<VkSemaphore, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (acquire) failed.");
        var acqSem = acqOk.Value;

        var presResult = vulkanContext.CreateSemaphore();
        if (presResult is not Result<VkSemaphore, VkApiResult>.Ok presOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (present) failed.");
        var presSem = presOk.Value;

        return new RenderGraph(vulkanContext, window, compiled, pool, cmdBuf, fence, acqSem, presSem, queue, queueFamily, imageMgr);
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
        _presentSemaphore.Dispose();
        _acquireSemaphore.Dispose();
        _inFlightFence.Dispose();
        _commandPool.Dispose();
    }
}
