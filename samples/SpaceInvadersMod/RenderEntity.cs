using System.Numerics;
using System.Runtime.InteropServices;

namespace SpaceInvadersMod;

[StructLayout(LayoutKind.Sequential)]
public struct RenderEntity
{
    public Vector2 Position;
    public uint EntityType; // 0=player, 1=enemy, 2=bullet
}
