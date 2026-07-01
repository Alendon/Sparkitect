using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

[Registry(Identifier = "ecs_system_group")]
[PublicAPI]
public sealed partial class SystemGroupRegistry : IRegistry<EcsModule>
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
