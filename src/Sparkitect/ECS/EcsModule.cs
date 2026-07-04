using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.ECS.Systems;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.ECS;

/// <summary>
/// State module that installs the ECS: registers the component, system-group, and system registries
/// and drives their registration passes on state entry and exit. Depends on the core module.
/// </summary>
[ModuleRegistry.RegisterModule("ecs")]
[PublicAPI]
public partial class EcsModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires => [];

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