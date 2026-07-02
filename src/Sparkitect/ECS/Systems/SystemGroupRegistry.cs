using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Registry for ECS system groups. Forwards each registered group to the <see cref="ISystemManager"/>
/// so it can take part in tree building and execution.
/// </summary>
[Registry(Identifier = "ecs_system_group")]
[PublicAPI]
public sealed partial class SystemGroupRegistry : IRegistry<EcsModule>
{
    /// <summary>The registry identifier used for registration wiring.</summary>
    public static string Identifier => "ecs_system_group";

    /// <summary>The system manager that receives registered groups; injected by the container.</summary>
    public required ISystemManager SystemManager { get; init; }

    /// <summary>Registers a system-group type under the given identification.</summary>
    [RegistryMethod]
    public void RegisterSystemGroup<TSystemGroup>(Identification id)
        where TSystemGroup : class, IHasIdentification
    {
        SystemManager.RegisterSystemGroup(id);
    }

    /// <summary>Unregisters a system group by identification.</summary>
    public void Unregister(Identification id)
    {
        // Group unregistration - no-op for now
    }
}
