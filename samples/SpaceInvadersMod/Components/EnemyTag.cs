using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("enemy_tag")]
public struct EnemyTag : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.EnemyTag;
}
