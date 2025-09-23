using Sparkitect.ECS;

namespace Sparkitect.GameState.Samples.Modules;


[ModuleRegistry.RegisterModule("ecs")]
[OrderModuleAfter<NetworkingModule>]
public sealed partial class EcsModule : IStateModule
{
    public static IReadOnlyList<Type> UsedServices => [typeof(IArchetypeManager), typeof(IComponentManager), typeof(ISystemManager)];
}