using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("shoot_cooldown")]
public struct ShootCooldown : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.ShootCooldown;

    public float Remaining;
}
