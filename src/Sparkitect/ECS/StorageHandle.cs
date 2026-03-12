using System.Runtime.InteropServices;

namespace Sparkitect.ECS;

/// <summary>
/// Opaque generational handle identifying a storage instance within a <see cref="World"/>.
/// The generation field prevents use-after-remove: if a storage is removed and its slot reused,
/// stale handles will fail validation.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint) * 2)]
public readonly struct StorageHandle : IEquatable<StorageHandle>
{
    /// <summary>
    /// Slot index into the World's parallel arrays.
    /// </summary>
    [FieldOffset(0)] public readonly uint Index;

    /// <summary>
    /// Generation counter for use-after-remove detection.
    /// </summary>
    [FieldOffset(sizeof(uint))] public readonly uint Generation;

    internal StorageHandle(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <inheritdoc/>
    public bool Equals(StorageHandle other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is StorageHandle other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Storage({Index}:{Generation})";
    }

    public static bool operator ==(StorageHandle left, StorageHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(StorageHandle left, StorageHandle right)
    {
        return !left.Equals(right);
    }
}
