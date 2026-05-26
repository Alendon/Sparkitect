using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Stateless;

namespace MinimalSampleMod;

public partial class SampleEntryState
{
    [TransitionFunction("create_render_graph")]
    [OnFrameEnterScheduling]
    [OrderAfter<Sparkitect.Graphics.RenderGraph.RenderGraphModule.ProcessRenderGraphRegistriesFunc>]
    public static void CreateRenderGraph(IMinimalSampleHost host) => host.Initialize();

    [PerFrameFunction("poll_window")]
    [PerFrameScheduling]
    public static void PollWindow(IMinimalSampleHost host) => host.PollEvents();

    [PerFrameFunction("check_window_closed")]
    [PerFrameScheduling]
    [OrderAfter<PollWindowFunc>]
    public static void CheckWindowClosed(IMinimalSampleHost host, IGameStateManager gameStateManager)
    {
        if (!host.IsOpen)
            gameStateManager.Shutdown();
    }

    [PerFrameFunction("run_render_graph_frame")]
    [PerFrameScheduling]
    [OrderAfter<SimulateEcsWorldFunc>]
    [OrderAfter<CheckWindowClosedFunc>]
    public static void RunRenderGraphFrame(IMinimalSampleHost host) => host.RunFrame();

    [TransitionFunction("destroy_render_graph")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.BeginVulkanTeardownFunc>]
    public static void DestroyRenderGraph(IMinimalSampleHost host) => host.Shutdown();
}
