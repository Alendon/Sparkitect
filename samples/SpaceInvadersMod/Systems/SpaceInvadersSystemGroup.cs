using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Systems;

[SystemGroupRegistry.RegisterSystemGroup("space_invaders")]
[SystemGroupScheduling]
public partial class SpaceInvadersSystemGroup : IHasIdentification
{
    public static Identification Identification => EcsSystemGroupID.SpaceInvadersMod.SpaceInvaders;
}
