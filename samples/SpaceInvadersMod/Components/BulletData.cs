using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("bullet_data")]
public struct BulletData : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.BulletData;

    public float Direction;
}
