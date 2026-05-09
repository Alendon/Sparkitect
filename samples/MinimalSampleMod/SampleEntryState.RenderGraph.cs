using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.DI;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.RenderGraph;
using Sparkitect.Stateless;
using Sparkitect.Windowing;

namespace MinimalSampleMod;

/// <summary>
/// Render-graph wiring partial for <see cref="SampleEntryState"/>. Owns the per-state
/// <see cref="RenderGraph"/> instance directly on this GameState class (Phase 49 D-E4 —
/// no host service / no <c>IMinimalRenderGraphHost</c>) and supplies the three lifecycle
/// transitions: create-graph (<see cref="OnCreateScheduling"/>), per-frame run-graph
/// (<see cref="PerFrameScheduling"/>), and destroy-graph
/// (<see cref="OnDestroyScheduling"/> + <c>[OrderBefore&lt;VulkanModule.BeginVulkanTeardownFunc&gt;]</c>
/// per D-C10).
/// </summary>
public partial class SampleEntryState
{
    // Per-state RenderGraph instance (D-E4 — owned directly on the GameState; no host service,
    // no IMinimalRenderGraphHost). Static because GameState transition / per-frame functions
    // are static (mirrors the existing TestCommandPool / PrintOnFrame pattern). The field's
    // lifetime is bounded by CreateRenderGraph / DestroyRenderGraph.
    private static RenderGraph? _renderGraph;

    [TransitionFunction("create_render_graph")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.CreateVmaFunc>]
    public static void CreateRenderGraph(
        IVulkanContext vulkanContext,
        IWindowManager windowManager,
        IDIService diService,
        IGameStateManager gameStateManager,
        IModManager modManager)
    {
        // Sample-local ownership of the window (D-C8). At WS scope MinimalSampleMod creates
        // (or reuses) a single 800x600 main window for the engine to drive.
        var window = windowManager.MainWindow ?? windowManager.CreateWindow("MinimalSampleMod", 800, 600);
        windowManager.MainWindow = window;

        Log.Information("Initializing RenderGraph with pass {PassId}",
            RenderPassID.MinimalSampleMod.ClearColor);

        _renderGraph = RenderGraph.Initialize(
            vulkanContext,
            window,
            diService,
            gameStateManager.CurrentCoreContainer,
            modManager,
            new List<Identification> { RenderPassID.MinimalSampleMod.ClearColor });
    }

    // [OrderAfter<SimulateEcsWorldFunc>] locks render-after-sim ordering at WS so the precedent
    // is in place for Phase 51+ when sim state actually drives render output (RESEARCH.md Open Q2
    // RESOLVED). SimulateEcsWorldFunc is the SG-emitted nested type from
    // [PerFrameFunction("simulate_ecs_world")] on SampleEntryState.SimulateWorld
    // (samples/MinimalSampleMod/SampleEntryState.Ecs.cs:17-21). Same partial-class scope, no
    // additional using required.
    [PerFrameFunction("run_render_graph_frame")]
    [PerFrameScheduling]
    [OrderAfter<SimulateEcsWorldFunc>]
    public static void RunRenderGraphFrame()
    {
        _renderGraph?.RunFrame();
    }

    // D-C10 (MUST): destroy MUST run BEFORE BeginVulkanTeardown so command pool / fence /
    // semaphores are released while the device is still alive.
    [TransitionFunction("destroy_render_graph")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.BeginVulkanTeardownFunc>]
    public static void DestroyRenderGraph()
    {
        _renderGraph?.Dispose();
        _renderGraph = null;
    }
}
