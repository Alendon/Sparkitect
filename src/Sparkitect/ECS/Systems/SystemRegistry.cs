using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[Registry(Identifier = "ecs_system", External = true)]
public sealed partial class SystemRegistry : StatelessFunctionRegistryBase, IRegistry
{
    public static string Identifier => "ecs_system";

    public required ISystemManager SystemManager { get; init; }

    public override void Register<TStatelessFunction>(Identification id)
    {
        base.Register<TStatelessFunction>(id);
        SystemManager.RegisterSystem(id);
    }
}
