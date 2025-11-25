using System.Diagnostics;
using System.Runtime.InteropServices;
using Sparkitect.Utils;

namespace Sparkitect.Modding;

/// <summary>
/// Compact 3-level hierarchical identifier (8 bytes total) for mod objects.
/// Maps to string identifiers (e.g., "sparkitect:blocks:stone") via <see cref="IIdentificationManager"/>.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
[DebuggerTypeProxy(typeof(IdentificationDebuggerProxy))]
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public readonly struct Identification : IEquatable<Identification>
{
    /// <summary>
    /// Numeric ID of the source mod.
    /// </summary>
    [FieldOffset(0)] public readonly ushort ModId;

    /// <summary>
    /// Numeric ID of the registry category.
    /// </summary>
    [FieldOffset(sizeof(ushort))] public readonly ushort CategoryId;

    /// <summary>
    /// Numeric ID of the item within the category.
    /// </summary>
    [FieldOffset(sizeof(ushort) * 2)] public readonly uint ItemId;

    /// <summary>
    /// Empty identification (0:0:0).
    /// </summary>
    public static readonly Identification Empty = Create(0, 0, 0);

    private Identification(ushort modId, ushort categoryId, uint itemId)
    {
        ModId = modId;
        CategoryId = categoryId;
        ItemId = itemId;
    }

    /// <summary>
    /// Creates an identification from numeric IDs.
    /// </summary>
    /// <param name="modId">The mod numeric ID.</param>
    /// <param name="categoryId">The category numeric ID.</param>
    /// <param name="itemId">The item numeric ID.</param>
    /// <returns>A new identification.</returns>
    public static Identification Create(ushort modId, ushort categoryId, uint itemId)
    {
        return new Identification(modId, categoryId, itemId);
    }

    /// <inheritdoc/>
    public bool Equals(Identification other)
    {
        return ModId == other.ModId && CategoryId == other.CategoryId && ItemId == other.ItemId;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Identification other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(ModId, CategoryId, ItemId);
    }

    private string GetDebuggerDisplay()
    {
        return IdentificationDebuggerProxy.FormatIdentification(this) ?? $"{ModId}:{CategoryId}:{ItemId}";
    }
}