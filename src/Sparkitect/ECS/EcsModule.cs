using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.ECS;

[ModuleRegistry.RegisterModule("ecs")]
[PublicAPI]
public partial class EcsModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    [OnFrameEnterScheduling]
    [TransitionFunction("process_ecs_registries_up")]
    static void ProcessRegistriesUp(IRegistryManager registryManager, ISystemManager systemManager)
    {
        registryManager.ProcessRegistry<UnmanagedComponentRegistry, EcsModule>();
        registryManager.ProcessRegistry<SystemGroupRegistry, EcsModule>();
        registryManager.ProcessRegistry<SystemRegistry, EcsModule>();
        systemManager.FetchMetadata();
    }

    [OnFrameExitScheduling]
    [TransitionFunction("process_ecs_registries_down")]
    static void ProcessRegistriesDown(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<UnmanagedComponentRegistry, EcsModule>();
        registryManager.ProcessRegistry<SystemGroupRegistry, EcsModule>();
        registryManager.ProcessRegistry<SystemRegistry, EcsModule>();
    }
}