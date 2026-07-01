using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Ledger;

/// <summary>One <c>(resource, epoch)</c> entry in the ledger: a single readable (or base) position of one resource chain. Its value identity is <see cref="Id"/>; provenance is metadata, never the identity.</summary>
[PublicAPI]
public sealed class LedgerNode
{
    private readonly List<GraphNodeId> _readers = [];

    internal LedgerNode(
        GraphNodeId id,
        GraphNodeId resource,
        Epoch epoch,
        Type resourceType,
        Identification provenance,
        GraphNodeId producingIncrementSource)
    {
        Id = id;
        Resource = resource;
        Epoch = epoch;
        ResourceType = resourceType;
        Provenance = provenance;
        ProducingIncrementSource = producingIncrementSource;
    }

    /// <summary>Value identity of this node, minted when the declaration was recorded.</summary>
    public GraphNodeId Id { get; }

    /// <summary>Identity of the resource chain this node belongs to (its declaring node).</summary>
    public GraphNodeId Resource { get; }

    /// <summary>The symbolic epoch this node represents within its resource chain.</summary>
    public Epoch Epoch { get; }

    /// <summary>The resource type carried by C# generic shape at declaration time.</summary>
    public Type ResourceType { get; }

    /// <summary>Declaration provenance (the declaring pass/description), metadata only.</summary>
    public Identification Provenance { get; }

    /// <summary>The node identity of the epoch this increment advanced from, or <see cref="GraphNodeId.None"/> for a base-epoch node.</summary>
    public GraphNodeId ProducingIncrementSource { get; }

    /// <summary>True for a base-epoch node — holdable, never readable, no producing increment.</summary>
    public bool IsBaseEpoch => Epoch.IsBase;

    /// <summary>The moment this increment is marked with, if any (only set by a marking record).</summary>
    public Identification MarkedMoment { get; private set; } = Identification.Empty;

    /// <summary>True when an increment producing this epoch has been marked with a moment.</summary>
    public bool IsMarked => !MarkedMoment.IsEmpty();

    /// <summary>The reader node identities recorded against this epoch.</summary>
    public IReadOnlyList<GraphNodeId> Readers => _readers;

    internal void AddReader(GraphNodeId reader) => _readers.Add(reader);

    internal void Mark(Identification moment) => MarkedMoment = moment;
}
