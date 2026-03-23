using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class SpaceInvadersState
{
    [TransitionFunction("si_render_init")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.ProcessRegistriesFunc>]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    static void InitializeRendering(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.InitializeRendering();
    }

    [PerFrameFunction("si_render_frame")]
    [PerFrameScheduling]
    [OrderAfter<CheckGameStateFunc>]
    static void RenderFrame(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.Render();
    }

    [TransitionFunction("si_render_cleanup")]
    [OnDestroyScheduling]
    [OrderBefore<VulkanModule.DestroyDeviceFunc>]
    static void CleanupRendering(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.CleanupRendering();
    }
}
