using Sparkitect.Modding;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Opaque identity for a render-graph resource slot, wrapping <c>(pass id, slot index)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GraphHandleId"/> is the stable tracking key that emergent managers use
/// to identify a specific resource slot on a specific pass across frames and
/// lifecycle phases. It is exposed on every <see cref="IGraphResource{T}"/> via the
/// <see cref="IGraphResource{T}.Id"/> property.
/// </para>
/// <para>
/// Slot index allocation is an emergent convention: source-generated and manually
/// authored pass categories allocate slot indices by positional counting of
/// <c>[GraphResource]</c>-annotated members (first annotated member is slot 0, second
/// is slot 1, and so on). The foundation exposes the slot index field but does not
/// allocate or validate it — enforcement lives in the stock source-generation
/// pipeline (Phases 58+) and companion analyzers (Phase 61).
/// </para>
/// <para>
/// The render-graph foundation is Vulkan-agnostic. Concrete
/// <see cref="IGraphResource{T}"/> implementations that construct
/// <see cref="GraphHandleId"/> instances are emitted by the source generator or
/// hand-written by authors of manual pass categories.
/// </para>
/// </remarks>
public readonly struct GraphHandleId : IEquatable<GraphHandleId>
{
    /// <summary>
    /// Identification of the pass that owns the slot this handle references.
    /// </summary>
    public readonly Identification PassId;

    /// <summary>
    /// Zero-based positional index of the slot within the pass's declared
    /// <c>[GraphResource]</c> members (first annotated member is slot 0).
    /// </summary>
    public readonly ushort SlotIndex;

    /// <summary>
    /// Initializes a new <see cref="GraphHandleId"/> with the specified pass
    /// identification and slot index.
    /// </summary>
    /// <param name="passId">The identification of the owning pass.</param>
    /// <param name="slotIndex">The zero-based positional slot index.</param>
    public GraphHandleId(Identification passId, ushort slotIndex)
    {
        PassId = passId;
        SlotIndex = slotIndex;
    }

    /// <inheritdoc/>
    public bool Equals(GraphHandleId other)
    {
        return PassId.Equals(other.PassId) && SlotIndex == other.SlotIndex;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is GraphHandleId other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(PassId, SlotIndex);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"GraphHandle({PassId}:{SlotIndex})";
    }

    public static bool operator ==(GraphHandleId left, GraphHandleId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GraphHandleId left, GraphHandleId right)
    {
        return !left.Equals(right);
    }
}
