using PongMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using Sparkitect.Windowing;

namespace PongMod;

[StateRegistry.RegisterState("pong")]
public partial class PongState : TransitiveGameState, IHasIdentification
{
    public override Identification ParentId => StateID.Sparkitect.Root;

    public override IReadOnlyList<Identification> DirectModules =>
    [
        StateModuleID.PongMod.Pong,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.RenderGraph,
        StateModuleID.Sparkitect.Windowing,
        StateModuleID.Sparkitect.Input
    ];

    [TransitionFunction("pong_init")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessShaderModuleRegistryEnterFunc>]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    [OrderBefore<RenderGraphModule.ProcessRenderGraphRegistriesEnterFunc>]
    public static void Initialize(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Initialize();
        Log.Information("Pong state initialized");
    }

    [TransitionFunction("pong_wire_input")]
    [OnCreateScheduling]
    [OrderAfter<InputModule.ProcessActionRegistryUpFunc>]
    [OrderAfter<PongInitFunc>]
    public static void WireInput(IPongRuntimeService pongRuntime) => pongRuntime.WireInput();

    [TransitionFunction("pong_create_graph")]
    [OnFrameEnterScheduling]
    [OrderAfter<RenderGraphModule.ProcessRenderGraphRegistriesEnterFunc>]
    public static void CreateGraph(IPongRuntimeService pongRuntime) => pongRuntime.CreateGraph();

    [PerFrameFunction("pong_frame")]
    [PerFrameScheduling]
    [OrderAfter<InputModule.InputProcessedFunc>]
    public static void Frame(IPongRuntimeService pongRuntime) => pongRuntime.Tick();

    [PerFrameFunction("pong_check_window_closed")]
    [PerFrameScheduling]
    [OrderAfter<WindowingModule.PumpWindowsFunc>]
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
    [OrderBefore<WindowingModule.WindowsTeardownFunc>]
    public static void Cleanup(IPongRuntimeService pongRuntime)
    {
        pongRuntime.Cleanup();
        Log.Information("Pong state cleanup");
    }
}
