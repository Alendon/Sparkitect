using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("shoot_cooldown")]
public partial struct ShootCooldown : IHasIdentification
{
    public float Remaining;
}
