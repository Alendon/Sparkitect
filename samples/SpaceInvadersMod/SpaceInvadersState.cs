using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod;

[StateRegistry.RegisterState("space_invaders")]
public partial class SpaceInvadersState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.SpaceInvadersMod.SpaceInvaders;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.SpaceInvadersMod.SpaceInvaders,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Windowing,
        StateModuleID.Sparkitect.Ecs
    ];
}
