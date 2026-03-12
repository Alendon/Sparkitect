using System.Runtime.InteropServices;

namespace Sparkitect.ECS;

/// <summary>
/// Opaque generational handle identifying an entity within a <see cref="World"/>.
/// The generation field prevents use-after-reclaim: if an entity is reclaimed and its slot
/// reused, stale EntityIds will fail validation.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint) * 2)]
public readonly struct EntityId : IEquatable<EntityId>
{
    /// <summary>
    /// Sentinel value representing no entity. Always invalid — generation 0 is never allocated.
    /// </summary>
    public static readonly EntityId None = default;

    /// <summary>
    /// Slot index into the World's entity parallel arrays.
    /// </summary>
    [FieldOffset(0)] internal readonly uint Index;

    /// <summary>
    /// Generation counter for use-after-reclaim detection.
    /// </summary>
    [FieldOffset(sizeof(uint))] internal readonly uint Generation;

    internal EntityId(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <summary>
    /// Returns true if this is the sentinel None value (default-constructed).
    /// </summary>
    public bool IsNone => Index == 0 && Generation == 0;

    /// <inheritdoc/>
    public bool Equals(EntityId other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EntityId other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Entity({Index}:{Generation})";
    }

    public static bool operator ==(EntityId left, EntityId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EntityId left, EntityId right)
    {
        return !left.Equals(right);
    }
}
