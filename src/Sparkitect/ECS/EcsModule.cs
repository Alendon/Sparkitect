using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.ECS;

[ModuleRegistry.RegisterModule("ecs")]
public partial class EcsModule : IStateModule
{
    public static Identification Identification => StateModuleID.Sparkitect.Ecs;
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    [TransitionFunction("add_ecs_registries")]
    [OnCreateScheduling]
    static void AddRegistries(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<UnmanagedComponentRegistry>();
        registryManager.AddRegistry<SystemGroupRegistry>();
        registryManager.AddRegistry<SystemRegistry>();
    }
    
    [OnFrameEnterScheduling]
    [TransitionFunction("process_ecs_registries_up")]
    [OrderAfter<AddEcsRegistriesFunc>]
    static void ProcessRegistriesUp(IRegistryManager registryManager, ISystemManager systemManager)
    {
        registryManager.ProcessAllMissing<UnmanagedComponentRegistry>();
        registryManager.ProcessAllMissing<SystemGroupRegistry>();
        registryManager.ProcessAllMissing<SystemRegistry>();
        systemManager.FetchMetadata();
    }
    
    
    [TransitionFunction("remove_ecs_registries")]
    [OnDestroyScheduling]
    static void RemoveRegistries(IRegistryManager registryManager)
    {
        //TODO add remove function
    }
    
    [OnFrameExitScheduling]
    [TransitionFunction("process_ecs_registries_down")]
    [OrderBefore<RemoveEcsRegistriesFunc>]
    static void ProcessRegistriesDown(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<UnmanagedComponentRegistry>();
        registryManager.UnregisterAllRemaining<SystemRegistry>();
    }
    
    
}