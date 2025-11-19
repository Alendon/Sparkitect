using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.ECS;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState.Samples.Modules;


[ModuleRegistry.RegisterModule("ecs")]
[OrderModuleAfter<NetworkingModule>]
public sealed partial class EcsModule : IStateModule
{
    public static Span<Identification> RequiredModules => [];
    public static Identification Identification => StateModuleID.Sparkitect.Ecs;
}