using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("bullet_data")]
public partial struct BulletData : IHasIdentification
{
    public float Direction;
}
