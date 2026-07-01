using System;
using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;

namespace SpaceInvadersMod.Resources;

/// <summary>
/// The published entity-list composite: the staged device <see cref="BufferResource"/> plus the element
/// <see cref="Count"/> that materializes at the producing pass's Execute and is then sealed. A no-manager
/// data composite — it carries the resolved buffer ref and the count only. The count is read off this
/// instance via the <c>entities_gpu</c> moment (never through DI), which makes the deprecated DI reach into
/// an entity-list manager structurally inexpressible.
/// </summary>
[PublicAPI]
public sealed class EntityListResource
{
    private int _count = -1;

    /// <summary>Composes the composite over the resolved device buffer; the count is unset until Execute.</summary>
    public EntityListResource(BufferResource buffer) => Buffer = buffer;

    /// <summary>The staged device buffer backing the entity list — the compute pass's shader-read storage buffer.</summary>
    public BufferResource Buffer { get; }

    /// <summary>The number of entities. Materialized once at the producing pass's Execute via <see cref="SetCount"/>, then sealed. Reading it before it is set is an error.</summary>
    public int Count => _count >= 0
        ? _count
        : throw new InvalidOperationException("EntityListResource.Count read before it was materialized.");

    /// <summary>
    /// Materializes and seals the element count (set-once). The ordering edge from the producing pass to any
    /// reader guarantees this write precedes the read, so a minimal set-once guard is sufficient.
    /// </summary>
    public void SetCount(int count)
    {
        if (_count >= 0)
            throw new InvalidOperationException("EntityListResource.Count is already sealed.");
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _count = count;
    }
}
