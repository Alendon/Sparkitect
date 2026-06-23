using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// Engine-neutral per-entity render element. 16-byte std430 stride (vec2 + uint + uint), matching the
/// shader-side <c>StructuredBuffer</c> element. The engine cannot reference a mod's element type, so a
/// supplier maps its own per-entity struct onto this at push time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[PublicAPI]
public struct GpuRenderEntity
{
    public Vector2 Position;
    public uint EntityType;
    private uint _padding;
}

/// <summary>
/// Pushable entity-list resource. Built per frame by <see cref="Create"/>, which rents a right-sized array
/// from a shared <see cref="ArrayPool{T}"/> and copies the supplied span in. Published through the
/// type-routed push door via <c>EntityListResourceExtensions.Apply</c>; its manager stores the current
/// instance and the staging pass reads <see cref="Elements"/> for the per-frame host-buffer fill.
/// Multiplicity (N entities) is an internal detail — the count-bounded <see cref="Elements"/> span is the
/// only external read surface.
/// </summary>
[ResourceManager<EntityListResourceManager>(true)]
[GraphResourceRegistry.RegisterResource("entity_list")]
[PublicAPI]
public sealed partial class EntityListResource : IHasIdentification
{
    private readonly GpuRenderEntity[] _rented;
    private readonly int _count;

    private EntityListResource(GpuRenderEntity[] rented, int count)
    {
        _rented = rented;
        _count = count;
    }

    /// <summary>Number of entities in this list.</summary>
    public int Count => _count;

    /// <summary>Count-bounded read view over the pooled backing — the staging pass's memcpy source.</summary>
    public ReadOnlySpan<GpuRenderEntity> Elements => _rented.AsSpan(0, _count);

    /// <summary>Byte size of the populated region (<c>Count * sizeof(GpuRenderEntity)</c>).</summary>
    public ulong ByteSize => (ulong)_count * (ulong)Marshal.SizeOf<GpuRenderEntity>();

    /// <summary>
    /// Builds a resource for <paramref name="elements"/>, renting a right-sized array from the shared pool
    /// and copying the span in. The complex per-entity transform stays supplier-side; this is pool-and-copy.
    /// </summary>
    public static EntityListResource Create(ReadOnlySpan<GpuRenderEntity> elements)
    {
        var rented = ArrayPool<GpuRenderEntity>.Shared.Rent(elements.Length);
        elements.CopyTo(rented);
        return new EntityListResource(rented, elements.Length);
    }

    /// <summary>Return the pooled backing after the staging copy has consumed it. Engine-internal.</summary>
    internal void ReturnToPool() => ArrayPool<GpuRenderEntity>.Shared.Return(_rented);
}
