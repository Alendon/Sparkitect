using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.CompilerGenerated.KeyedFactoryExtensions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Graphics.RenderGraph.Hooks;
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
/// <remarks>
/// Passes receive the swapchain through a per-graph
/// <see cref="RenderGraphResolutionProvider"/>; the host container handles everything else.
/// </remarks>
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
        uint graphicsQueueFamily)
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
    }

    /// <summary>
    /// Builds a fully initialized, ready-to-<see cref="RunFrame"/> render graph.
    /// </summary>
    public static RenderGraph Initialize(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        IDIService diService,
        ICoreContainer hostContainer,
        IModManager modManager,
        IReadOnlyList<Identification> passIds)
    {
        var resolutionProvider = new RenderGraphResolutionProvider(window, window.Swapchain);

        var modIdList = modManager.LoadedMods.Select(m => m.Id).ToList();
        var passFactory = RenderPassRegistry.BuildRegisterPassContainer(
            diService,
            hostContainer,
            resolutionProvider,
            modIdList);

        var compiler = new RenderGraphCompiler();
        foreach (var id in passIds)
        {
            if (!passFactory.TryResolve(id, out var pass))
                throw new InvalidOperationException(
                    $"No render pass registered for {id} — call RenderPassRegistry.RegisterPass<…>(id) first.");
            ((ISetupHook)pass).Setup();
            compiler.AddPass(id, pass);
        }
        var compiled = compiler.Compile();

        var (queueFamily, queue) = ResolveGraphicsQueue(vulkanContext);
        
        
        var poolResult = vulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit, queueFamily);
        if (poolResult is not Result<VkCommandPool, VkApiResult>.Ok poolOk)
            throw new InvalidOperationException("RenderGraph: CreateCommandPool failed.");
        var pool = poolOk.Value;

        var cmdResult = pool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (cmdResult is not Result<VkCommandBuffer, VkApiResult>.Ok cmdOk)
            throw new InvalidOperationException("RenderGraph: AllocateCommandBuffer failed.");
        var cmdBuf = cmdOk.Value;

        // Fence MUST start signaled so the first RunFrame's Wait() returns immediately.
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

        return new RenderGraph(vulkanContext, window, compiled, pool, cmdBuf, fence, acqSem, presSem, queue, queueFamily);
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

        // Wait for device idle so per-frame objects are not in use when destroyed.
        unsafe { _vulkanContext.VkApi.DeviceWaitIdle(_vulkanContext.VkDevice.Handle); }

        _presentSemaphore.Dispose();
        _acquireSemaphore.Dispose();
        _inFlightFence.Dispose();
        // Command buffer auto-freed when pool destroyed.
        _commandPool.Dispose();
    }
}
