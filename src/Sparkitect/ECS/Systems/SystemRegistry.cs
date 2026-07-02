using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Registry for ECS system functions. Extends the stateless-function registry base and additionally
/// notifies the <see cref="ISystemManager"/> of each registered system.
/// </summary>
[Registry(Identifier = "ecs_system", External = true)]
[PublicAPI]
public sealed partial class SystemRegistry : StatelessFunctionRegistryBase, IRegistry<EcsModule>
{
    /// <summary>The registry identifier used for registration wiring.</summary>
    public static string Identifier => "ecs_system";

    /// <summary>The system manager notified of each registered system; injected by the container.</summary>
    public required ISystemManager SystemManager { get; init; }

    /// <summary>Registers a system wrapper type and marks it as a system on the manager.</summary>
    public override void Register<TStatelessFunction>(Identification id)
    {
        base.Register<TStatelessFunction>(id);
        SystemManager.RegisterSystem(id);
    }
}
