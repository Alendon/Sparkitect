using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("player_tag")]
public struct PlayerTag : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.PlayerTag;
}
