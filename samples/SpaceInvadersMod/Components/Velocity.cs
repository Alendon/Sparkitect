using System.Numerics;
using SpaceInvadersMod.CompilerGenerated.IdExtensions;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace SpaceInvadersMod.Components;

[UnmanagedComponentRegistry.RegisterComponent("velocity")]
public struct Velocity : IHasIdentification
{
    public static Identification Identification => UnmanagedComponentID.SpaceInvadersMod.Velocity;

    public Vector2 Value;
}
