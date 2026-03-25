using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Systems;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace SpaceInvadersMod.Systems;

[SystemGroupRegistry.RegisterSystemGroup("gameplay")]
[SystemGroupScheduling]
[ParentId<SpaceInvadersSystemGroup>]
public partial class GameplayGroup : IHasIdentification
{
    public static Identification Identification => EcsSystemGroupID.SpaceInvadersMod.Gameplay;
}
