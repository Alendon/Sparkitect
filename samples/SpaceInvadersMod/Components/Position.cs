using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("position")]
public struct Position : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.Position;

    public Vector2 Value;
}
