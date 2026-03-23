using System.Numerics;
using System.Runtime.InteropServices;

namespace SpaceInvadersMod;

/// <summary>
/// Push constant data for the Space Invaders compute shader.
/// Layout must match the shader's SpaceInvadersGameData struct exactly.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpaceInvadersGameData
{
    public uint EntityCount;
    public uint ScreenWidth;
    public uint ScreenHeight;
    public float Padding; // Alignment padding to keep Vector3 at offset 16
    public Vector3 BackgroundColor;
}
