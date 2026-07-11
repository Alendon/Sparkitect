using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Stateless;
using Sparkitect.Windowing;

namespace MinimalSampleMod;

public partial class SampleEntryState
{
    [TransitionFunction("create_render_graph")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessShaderModuleRegistryEnterFunc>]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    [OrderAfter<Sparkitect.Graphics.RenderGraph.RenderGraphModule.ProcessRenderGraphRegistriesEnterFunc>]
    public static void CreateRenderGraph(IMinimalSampleHost host) => host.Initialize();

    [PerFrameFunction("check_window_closed")]
    [PerFrameScheduling]
    [OrderAfter<WindowingModule.PumpWindowsFunc>]
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
    [OrderBefore<WindowingModule.WindowsTeardownFunc>]
    public static void DestroyRenderGraph(IMinimalSampleHost host) => host.Shutdown();
}
