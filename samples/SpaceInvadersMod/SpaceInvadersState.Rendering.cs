using Sparkitect.GameState;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class SpaceInvadersState
{
    [TransitionFunction("si_init")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessRegistriesFunc>]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    [OrderBefore<RenderGraphModule.ProcessRenderGraphRegistriesFunc>]
    static void Initialize(ISpaceInvadersRuntimeServiceStateFacade manager) => manager.Initialize();

    [TransitionFunction("si_create_graph")]
    [OnFrameEnterScheduling]
    [OrderAfter<RenderGraphModule.ProcessRenderGraphRegistriesFunc>]
    static void CreateGraph(ISpaceInvadersRuntimeServiceStateFacade manager) => manager.CreateGraph();

    [PerFrameFunction("si_check_window_closed")]
    [PerFrameScheduling]
    static void CheckWindowClosed(ISpaceInvadersRuntimeService runtime, IGameStateManager gameStateManager)
    {
        if (!runtime.IsOpen)
            gameStateManager.Shutdown();
    }

    [PerFrameFunction("si_run_graph_frame")]
    [PerFrameScheduling]
    [OrderAfter<CheckGameStateFunc>]
    [OrderAfter<SiCheckWindowClosedFunc>]
    static void RunGraphFrame(ISpaceInvadersRuntimeServiceStateFacade manager) => manager.RunFrame();

    [TransitionFunction("si_destroy_graph")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.BeginVulkanTeardownFunc>]
    static void DestroyGraph(ISpaceInvadersRuntimeServiceStateFacade manager) => manager.ShutdownGraph();

    [TransitionFunction("si_cleanup")]
    [OnDestroyScheduling]
    [OrderAfter<SiDestroyGraphFunc>]
    [OrderBefore<VulkanModule.DestroyDeviceFunc>]
    static void Cleanup(ISpaceInvadersRuntimeServiceStateFacade manager) => manager.Cleanup();
}
