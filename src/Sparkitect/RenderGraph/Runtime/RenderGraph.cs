using System.Reflection;
using Silk.NET.Vulkan;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Windowing;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Walking-skeleton stock render graph. Frame-driving (owns command pool, command buffer,
/// in-flight fence, acquire and present semaphores) but externally invoked — consumers
/// (e.g. a GameState per-frame function) call <see cref="RunFrame"/>.
/// </summary>
/// <remarks>
/// <para>
/// Per Phase 49 D-E1, instances are constructed exclusively via the static
/// <see cref="Initialize"/> factory — there is no public constructor. Mirrors the
/// <c>IWorld.Create()</c> posture (D-D6).
/// </para>
/// <para>
/// Per D-E3, the swapchain is taken as a constructor parameter and distributed to passes
/// that depend on it via a per-graph custom <see cref="Sparkitect.DI.Resolution.IResolutionProvider"/>
/// (<see cref="RenderGraphResolutionProvider"/>) built inside <see cref="Initialize"/>. The
/// provider is passed as the <c>provider</c> argument to
/// <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>; the host container is passed
/// UNWRAPPED as the <c>container</c> argument. There is no global current-swapchain-image DI
/// service (D-E3); passes that need the current acquired image read it through
/// <see cref="IRenderGraphFrameContext"/>, also distributed through the resolution provider.
/// </para>
/// <para>
/// Per D-E4, there is NO singleton host service — consumers create and own
/// <see cref="RenderGraph"/> instances directly (e.g. as a per-state field on a GameState class).
/// </para>
/// <para>
/// Walking-skeleton ships a single concrete <see cref="RenderGraph"/> stock class with no
/// engine-facing interface (D-D6). Future graph variants (e.g. <c>ComputeGraph</c>, D-D8)
/// would be separate stock classes reusing the shared infrastructure (pass registry,
/// keyed-factory configurator, hook interfaces).
/// </para>
/// </remarks>
public sealed partial class RenderGraph : IDisposable
{
    /// <summary>
    /// Conventional simple-name of the per-mod configurator attribute the 49.2 generator
    /// emits for <see cref="RenderPassRegistry"/>'s <c>RegisterPass</c> method. The
    /// attribute lives in each consuming mod's <c>{ModNs}.CompilerGenerated</c> namespace
    /// (per <c>gen/Sparkitect.Generator/Modding/RegistryGenerator.Output.cs</c>) — NOT in
    /// <c>Sparkitect.dll</c>. <see cref="Initialize"/> resolves it at runtime via reflection
    /// across loaded mod assemblies (deviation from the plan's compile-time
    /// <c>typeof(...)</c> form, which cannot reference a type not yet emitted into
    /// <c>Sparkitect.dll</c>).
    /// </summary>
    private const string KeyedFactoryConfiguratorAttributeSimpleName =
        "RenderPassRegistry_RegisterPass_KeyedFactoryConfiguratorAttribute";

    private readonly IVulkanContext _vulkanContext;
    private readonly ISparkitWindow _window;
    private readonly CompiledRenderGraph _compiled;
    private readonly RenderGraphFrameContext _frameContext;
    private readonly VkCommandPool _commandPool;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkFence _inFlightFence;
    private readonly VkSemaphore _acquireSemaphore;
    private readonly VkSemaphore _presentSemaphore;
    private readonly Queue _graphicsQueue;
    private readonly uint _graphicsQueueFamily;
    private bool _disposed;

    private RenderGraph(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        CompiledRenderGraph compiled,
        RenderGraphFrameContext frameContext,
        VkCommandPool commandPool,
        VkCommandBuffer commandBuffer,
        VkFence inFlightFence,
        VkSemaphore acquireSemaphore,
        VkSemaphore presentSemaphore,
        Queue graphicsQueue,
        uint graphicsQueueFamily)
    {
        _vulkanContext = vulkanContext;
        _window = window;
        _compiled = compiled;
        _frameContext = frameContext;
        _commandPool = commandPool;
        _commandBuffer = commandBuffer;
        _inFlightFence = inFlightFence;
        _acquireSemaphore = acquireSemaphore;
        _presentSemaphore = presentSemaphore;
        _graphicsQueue = graphicsQueue;
        _graphicsQueueFamily = graphicsQueueFamily;
    }

    /// <summary>
    /// Builds a fully initialized, ready-to-<see cref="RunFrame"/> render graph (D-E1).
    /// </summary>
    public static RenderGraph Initialize(
        IVulkanContext vulkanContext,
        ISparkitWindow window,
        IDIService diService,
        ICoreContainer hostContainer,
        IModManager modManager,
        IReadOnlyList<Identification> passIds)
    {
        ArgumentNullException.ThrowIfNull(vulkanContext);
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(diService);
        ArgumentNullException.ThrowIfNull(hostContainer);
        ArgumentNullException.ThrowIfNull(modManager);
        ArgumentNullException.ThrowIfNull(passIds);

        // 1. Build the per-graph custom IResolutionProvider (D-E3 step 3) that returns
        //    the per-graph ISparkitWindow / VkSwapchain / IRenderGraphFrameContext and
        //    falls through (returns false) for everything else. ResolutionScope then
        //    falls back to the host container per ResolutionScope.cs:46-52.
        var frameContext = new RenderGraphFrameContext();
        var resolutionProvider = new RenderGraphResolutionProvider(window, window.Swapchain, frameContext);

        // 2. Resolve the per-mod configurator attribute Type at runtime by simple name.
        //    The 49.2 generator emits <ModNs>.CompilerGenerated.RenderPassRegistry_RegisterPass_KeyedFactoryConfiguratorAttribute
        //    only into mod assemblies that contain at least one [RenderPassRegistry.RegisterPass(...)]
        //    -attributed concrete class — Sparkitect.dll itself never carries the attribute,
        //    so a compile-time typeof(...) reference is impossible. At walking-skeleton scope a
        //    single mod (MinimalSampleMod, Wave 3) ships pass concretes; resolving against any
        //    one matching attribute Type drives BuildFactoryContainer's per-mod entrypoint scan.
        //    Multi-mod-with-passes refinement deferred (Wave 3+ revisits if/when needed).
        var modIdList = modManager.LoadedMods.Select(m => m.Id).ToList();
        var configuratorAttributeType = ResolveKeyedFactoryConfiguratorAttributeType(modIdList);

        // 3. Build the keyed-factory container for IPass — mirror DummyValueManager.cs:54-58,
        //    BUT pass our resolutionProvider as the provider arg (DummyValueManager passes null)
        //    and pass hostContainer UNWRAPPED as the container arg (NO ICoreContainer wrapping
        //    per D-E3 step 4). Pass ctors that ask for ISparkitWindow / VkSwapchain /
        //    IRenderGraphFrameContext resolve through the provider; everything else resolves
        //    through the host container fallback at ResolutionScope.cs:46-52.
        var passFactory = diService.BuildFactoryContainer<Identification, IPass>(
            hostContainer,
            resolutionProvider,
            modIdList,
            configuratorAttributeType);

        // 4. Resolve passes by id, hand-dispatch Setup(), and feed compiler. Fail-fast on missing id.
        var compiler = new RenderGraphCompiler();
        foreach (var id in passIds)
        {
            if (!passFactory.TryResolve(id, out var pass))
                throw new InvalidOperationException(
                    $"No render pass registered for {id} — call RenderPassRegistry.RegisterPass<…>(id) first.");
            ((ISetupHook)pass).Setup();   // D-D10 hand-dispatch
            compiler.AddPass(id, pass);
        }
        var compiled = compiler.Compile();

        // 5. Resolve graphics queue family + queue (mirror samples/PongMod/PongRuntimeService.cs:272 —
        //    use the existing VkPhysicalDevice.GetQueueFamilyProperties() wrapper, NOT a hand-rolled
        //    stackalloc reimplementation; the property is named `PhysicalDevice`, NOT `Handle`).
        var (queueFamily, queue) = ResolveGraphicsQueue(vulkanContext);

        // 6. Allocate per-frame Vulkan objects via IVulkanContext factories (D-C6).
        var poolResult = vulkanContext.CreateCommandPool(
            CommandPoolCreateFlags.ResetCommandBufferBit, queueFamily);
        if (poolResult is not Sparkitect.Utils.DU.Result<VkCommandPool, VkApiResult>.Ok poolOk)
            throw new InvalidOperationException("RenderGraph: CreateCommandPool failed.");
        var pool = poolOk.Value;

        var cmdResult = pool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (cmdResult is not Sparkitect.Utils.DU.Result<VkCommandBuffer, VkApiResult>.Ok cmdOk)
            throw new InvalidOperationException("RenderGraph: AllocateCommandBuffer failed.");
        var cmdBuf = cmdOk.Value;

        // Fence MUST start signaled so the first RunFrame's Wait() returns immediately.
        var fenceResult = vulkanContext.CreateFence(FenceCreateFlags.SignaledBit);
        if (fenceResult is not Sparkitect.Utils.DU.Result<VkFence, VkApiResult>.Ok fenceOk)
            throw new InvalidOperationException("RenderGraph: CreateFence failed.");
        var fence = fenceOk.Value;

        var acqResult = vulkanContext.CreateSemaphore();
        if (acqResult is not Sparkitect.Utils.DU.Result<VkSemaphore, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (acquire) failed.");
        var acqSem = acqOk.Value;

        var presResult = vulkanContext.CreateSemaphore();
        if (presResult is not Sparkitect.Utils.DU.Result<VkSemaphore, VkApiResult>.Ok presOk)
            throw new InvalidOperationException("RenderGraph: CreateSemaphore (present) failed.");
        var presSem = presOk.Value;

        return new RenderGraph(vulkanContext, window, compiled, frameContext, pool, cmdBuf, fence, acqSem, presSem, queue.Handle, queueFamily);
    }

    /// <summary>
    /// Resolves the per-mod
    /// <c>RenderPassRegistry_RegisterPass_KeyedFactoryConfiguratorAttribute</c> Type by
    /// simple-name across loaded mod assemblies. The 49.2 generator emits this attribute
    /// into the assembly that contains at least one
    /// <c>[RenderPassRegistry.RegisterPass(...)]</c>-attributed concrete; the attribute
    /// is <c>internal sealed</c> in the mod's <c>{ModNs}.CompilerGenerated</c> namespace.
    /// </summary>
    /// <remarks>
    /// Walking-skeleton scope assumes a single consuming mod (MinimalSampleMod). If multiple
    /// loaded mods register passes, this returns the first found Type — the others' configurators
    /// will not run for this graph instance. Multi-mod-with-passes is a deferred enhancement.
    /// </remarks>
    private static Type ResolveKeyedFactoryConfiguratorAttributeType(IReadOnlyList<string> modIdList)
    {
        // Scan all loaded assemblies (DIService internally maps modIds -> assemblies, but doesn't
        // expose the map; AppDomain scan is acceptable here since attribute Types are per-assembly
        // unique by simple name and the cost is one-time at graph init).
        Type? found = null;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip assemblies that obviously won't carry compiler-generated mod attributes.
            // Specifically, dynamic and uninspectable assemblies are skipped quietly; the rest
            // we probe via GetTypes() inside a try/catch (a single dependency-load failure
            // should not abort RG initialization for the entire process).
            if (assembly.IsDynamic) continue;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(static t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var t in types)
            {
                if (t.Name == KeyedFactoryConfiguratorAttributeSimpleName)
                {
                    found = t;
                    break;
                }
            }
            if (found is not null) break;
        }

        if (found is null)
            throw new InvalidOperationException(
                $"RenderGraph.Initialize: no '{KeyedFactoryConfiguratorAttributeSimpleName}' attribute Type found in any loaded assembly. " +
                "At least one loaded mod must carry a [RenderPassRegistry.RegisterPass(...)]-attributed concrete pass class " +
                "so the 49.2 keyed-factory generator emits the per-mod configurator + attribute. " +
                $"Loaded mod ids: {string.Join(", ", modIdList)}.");

        return found;
    }

    /// <summary>
    /// Mirrors <c>samples/PongMod/PongRuntimeService.cs:272</c> — uses the existing
    /// <see cref="VkPhysicalDevice.GetQueueFamilyProperties"/> wrapper. Per RESEARCH Open Q3,
    /// duplicating inline at WS rather than promoting to <see cref="IVulkanContext"/>.
    /// </summary>
    private static (uint family, VulkanQueue queue) ResolveGraphicsQueue(IVulkanContext ctx)
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

        // Disposes registered through IObjectTracker<VulkanObject> (existing pattern).
        _presentSemaphore.Dispose();
        _acquireSemaphore.Dispose();
        _inFlightFence.Dispose();
        _commandPool.Dispose();   // command buffer auto-freed when pool destroyed (verified VkCommandPool.Destroy semantics).
    }
}
