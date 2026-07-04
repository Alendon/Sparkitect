using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod;

[ModuleRegistry.RegisterModule("space_invaders")]
public partial class SpaceInvadersModule : TransitiveStateModule, IHasIdentification
{
    public override IReadOnlyList<Identification> Requires =>
    [
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Windowing
    ];
}
