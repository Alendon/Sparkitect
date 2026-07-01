using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace SpaceInvadersMod;

/// <summary>
/// Per-entity render element, 16-byte std430 stride (vec2 + uint + uint), matching the shader-side
/// storage-buffer element. Bit-compatible with <see cref="RenderEntity"/> so the runtime service maps the
/// ECS list onto it via <c>MemoryMarshal.Cast</c> at push time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[PublicAPI]
public struct GpuRenderEntity
{
    public Vector2 Position;
    public uint EntityType;
    private uint _padding;
}
