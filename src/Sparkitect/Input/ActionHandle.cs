using JetBrains.Annotations;
using System.Runtime.InteropServices;

namespace Sparkitect.Input;

/// <summary>
/// Opaque generational handle over a per-action result slot (D-18/Pattern 4, mirrors
/// <see cref="Sparkitect.ECS.StorageHandle"/>). Points at a RESULT slot for a pure typed read —
/// it never re-runs binding evaluation. The generation field guards against slot reuse when an
/// action is unregistered/re-registered across a mod (un)load.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = sizeof(uint) * 2)]
[PublicAPI]
public readonly struct ActionHandle : IEquatable<ActionHandle>
{
    /// <summary>
    /// Slot index into the InputManager's per-action result-slot array.
    /// </summary>
    [FieldOffset(0)] public readonly uint Index;

    /// <summary>
    /// Generation counter for use-after-invalidate detection.
    /// </summary>
    [FieldOffset(sizeof(uint))] public readonly uint Generation;

    internal ActionHandle(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <inheritdoc/>
    public bool Equals(ActionHandle other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ActionHandle other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"Action({Index}:{Generation})";
    }

    /// <summary>Returns true when both handles share the same index and generation.</summary>
    public static bool operator ==(ActionHandle left, ActionHandle right)
    {
        return left.Equals(right);
    }

    /// <summary>Returns true when the handles differ in index or generation.</summary>
    public static bool operator !=(ActionHandle left, ActionHandle right)
    {
        return !left.Equals(right);
    }
}
