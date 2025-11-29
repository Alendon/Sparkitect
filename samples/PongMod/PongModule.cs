using PongMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace PongMod;

[ModuleRegistry.RegisterModule("pong")]
public partial class PongModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules =>
    [
        StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Windowing
    ];

    public static Identification Identification => StateModuleID.PongMod.Pong;
}
