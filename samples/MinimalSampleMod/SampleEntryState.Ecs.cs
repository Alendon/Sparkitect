using Sparkitect.ECS;
using Sparkitect.GameState;
using Sparkitect.Stateless;

namespace MinimalSampleMod;

public partial class SampleEntryState
{
    [TransitionFunction("create_ecs_world")]
    [OnCreateScheduling]
    [OrderAfter<EcsModule.ProcessEcsRegistriesUpFunc>]
    static void CreateWorld(IDummyValueManagerStateFacade  manager)
    {
        manager.BuildWorld();
    }

    [PerFrameFunction("simulate_ecs_world")]
    [PerFrameScheduling]
    static void SimulateWorld(IDummyValueManagerStateFacade  manager)
    {
        manager.SimulateWorld();
    }

    [TransitionFunction("destroy_ecs_world")]
    [OnDestroyScheduling]
    [OrderBefore<EcsModule.ProcessEcsRegistriesDownFunc>]
    static void DestroyWorld(IDummyValueManagerStateFacade  manager)
    {
        manager.DestroyWorld();
    }
}