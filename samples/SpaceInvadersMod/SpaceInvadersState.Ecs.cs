using Sparkitect.ECS;
using Sparkitect.GameState;
using Sparkitect.Stateless;

namespace SpaceInvadersMod;

public partial class SpaceInvadersState
{
    [TransitionFunction("create_ecs_world")]
    [OnCreateScheduling]
    [OrderAfter<EcsModule.ProcessEcsRegistriesUpFunc>]
    static void CreateWorld(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.BuildWorld();
    }

    [PerFrameFunction("simulate_ecs_world")]
    [PerFrameScheduling]
    static void SimulateWorld(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.SimulateWorld();
    }

    [TransitionFunction("destroy_ecs_world")]
    [OnDestroyScheduling]
    [OrderBefore<EcsModule.ProcessEcsRegistriesDownFunc>]
    static void DestroyWorld(ISpaceInvadersRuntimeServiceStateFacade manager)
    {
        manager.DestroyWorld();
    }
}
