using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("position")]
public partial struct Position : IHasIdentification
{
    public Vector2 Value;
}
