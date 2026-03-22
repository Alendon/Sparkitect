using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod;

[ModuleRegistry.RegisterModule("space_invaders")]
public partial class SpaceInvadersModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules =>
    [
        StateModuleID.Sparkitect.Core,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Windowing
    ];

    public static Identification Identification => StateModuleID.SpaceInvadersMod.SpaceInvaders;
}
