using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("velocity")]
public partial struct Velocity : IHasIdentification
{
    public Vector2 Value;
}
