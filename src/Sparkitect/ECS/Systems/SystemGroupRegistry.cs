using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

[Registry(Identifier = "ecs_system_group")]
public sealed partial class SystemGroupRegistry : IRegistry
{
    public static string Identifier => "ecs_system_group";

    public required ISystemManager SystemManager { get; init; }

    [RegistryMethod]
    public void RegisterSystemGroup<TSystemGroup>(Identification id)
        where TSystemGroup : class, IHasIdentification
    {
        SystemManager.RegisterSystemGroup(id);
    }

    public void Unregister(Identification id)
    {
        // Group unregistration - no-op for now
    }
}
