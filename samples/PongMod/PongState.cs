using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace PongMod;

[StateRegistry.RegisterState("pong")]
public partial class PongState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.PongMod.Pong,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.RenderGraph,
        StateModuleID.Sparkitect.Windowing
    ];

    [TransitionFunction("pong_init")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessRegistriesFunc>]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    [OrderBefore<RenderGraphModule.ProcessRenderGraphRegistriesFunc>]
    public static void Initialize(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Initialize();
        Log.Information("Pong state initialized");
    }

    [TransitionFunction("pong_create_graph")]
    [OnFrameEnterScheduling]
    [OrderAfter<RenderGraphModule.ProcessRenderGraphRegistriesFunc>]
    public static void CreateGraph(IPongRuntimeService pongRuntime) => pongRuntime.CreateGraph();

    [PerFrameFunction("pong_frame")]
    [PerFrameScheduling]
    public static void Frame(IPongRuntimeService pongRuntime) => pongRuntime.Tick();

    [PerFrameFunction("pong_poll_window")]
    [PerFrameScheduling]
    public static void PollWindow(IPongRuntimeService pongRuntime) => pongRuntime.PollWindow();

    [PerFrameFunction("pong_check_window_closed")]
    [PerFrameScheduling]
    [OrderAfter<PongPollWindowFunc>]
    public static void CheckWindowClosed(IPongRuntimeService pongRuntime, IGameStateManager gameStateManager)
    {
        if (!pongRuntime.IsOpen)
            gameStateManager.Shutdown();
    }

    [PerFrameFunction("pong_run_graph_frame")]
    [PerFrameScheduling]
    [OrderAfter<PongFrameFunc>]
    [OrderAfter<PongCheckWindowClosedFunc>]
    public static void RunGraphFrame(IPongRuntimeService pongRuntime) => pongRuntime.RunFrame();

    [TransitionFunction("pong_destroy_graph")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.BeginVulkanTeardownFunc>]
    public static void DestroyGraph(IPongRuntimeService pongRuntime) => pongRuntime.ShutdownGraph();

    [TransitionFunction("pong_cleanup")]
    [OnDestroyScheduling]
    [OrderAfter<PongDestroyGraphFunc>]
    [OrderBefore<VulkanModule.DestroyDeviceFunc>]
    public static void Cleanup(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Cleanup();
        Log.Information("Pong state cleanup");
    }
}
