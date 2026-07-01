using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Ledger;

/// <summary>
/// The single compile truth: it stores ledger nodes and per-resource epoch chains, mints opaque
/// <see cref="ResourceRef{T}"/>s, records the two relations (Read and Increment) plus moment
/// markings, and exposes the query surface compilation reads. Refs are handed out eagerly but
/// unresolved — epoch positions are symbolic here; resolution to concrete positions happens later,
/// in the Link phase, so a declaration's outcome never depends on pass-setup order.
/// </summary>
[PublicAPI]
public sealed class DeclarationLedger
{
    private readonly List<LedgerNode> _nodes = [];
    private readonly Dictionary<GraphNodeId, List<LedgerNode>> _chainsByResource = [];
    private readonly List<ReadEdge> _reads = [];
    private readonly List<IncrementEdge> _increments = [];
    private readonly List<MomentReadEdge> _momentReads = [];
    private int _nodeOrdinal;

    /// <summary>All ledger nodes in mint order.</summary>
    public IReadOnlyList<LedgerNode> Nodes => _nodes;

    /// <summary>The recorded Read edges (a reader consuming a resource at one epoch).</summary>
    public IReadOnlyList<ReadEdge> Reads => _reads;

    /// <summary>The recorded Increment edges (a resource advanced from one epoch to the next).</summary>
    public IReadOnlyList<IncrementEdge> Increments => _increments;

    /// <summary>The recorded moment-reference edges (a consume node referencing a moment by id).</summary>
    public IReadOnlyList<MomentReadEdge> MomentReads => _momentReads;

    /// <summary>
    /// Declares a new resource: mints its base-epoch node and returns a base-epoch reference. The
    /// base epoch is holdable but never readable — it has no producing increment until incremented.
    /// </summary>
    /// <typeparam name="T">The resource type, carried onto the node and the minted reference.</typeparam>
    /// <param name="provenance">The declaring pass/description identity, recorded as node metadata.</param>
    public ResourceRef<T> Declare<T>(Identification provenance)
    {
        var resource = MintNodeId();
        var node = new LedgerNode(
            id: resource,
            resource: resource,
            epoch: Epoch.Base,
            resourceType: typeof(T),
            provenance: provenance,
            producingIncrementSource: GraphNodeId.None);

        _nodes.Add(node);
        _chainsByResource[resource] = [node];
        return new ResourceRef<T>(resource, Epoch.Base);
    }

    /// <summary>
    /// Records a Read of an already-minted reference by the given reader, registering a reader edge
    /// against that epoch's node.
    /// </summary>
    public void RecordRead<T>(ResourceRef<T> reference, GraphNodeId reader)
    {
        var node = NodeFor(reference.Resource, reference.Epoch);
        node.AddReader(reader);
        _reads.Add(new ReadEdge(node.Id, reader, reference.Resource, reference.Epoch));
    }

    /// <summary>
    /// Records an Increment advancing the referenced resource one epoch. Mints the post-increment
    /// node and reference (the same mechanic whether the target is a sub-resource or the resource
    /// the declaration itself resolves to) and returns the new reference.
    /// </summary>
    /// <param name="reference">The source-epoch reference being advanced.</param>
    /// <param name="provenance">The increment's provenance, recorded as the new node's metadata.</param>
    public ResourceRef<T> RecordIncrement<T>(ResourceRef<T> reference, Identification provenance)
    {
        var source = NodeFor(reference.Resource, reference.Epoch);
        var nextEpoch = reference.Epoch.Next();
        var nextNode = new LedgerNode(
            id: MintNodeId(),
            resource: reference.Resource,
            epoch: nextEpoch,
            resourceType: typeof(T),
            provenance: provenance,
            producingIncrementSource: source.Id);

        _nodes.Add(nextNode);
        _chainsByResource[reference.Resource].Add(nextNode);
        _increments.Add(new IncrementEdge(source.Id, nextNode.Id, reference.Resource, reference.Epoch, nextEpoch));
        return new ResourceRef<T>(reference.Resource, nextEpoch);
    }

    /// <summary>
    /// Marks the increment producing the referenced epoch with a moment. Marking is publishing: the
    /// moment thereafter resolves to this increment's result epoch. Marking the base epoch is
    /// meaningless (it has no producing increment) and is rejected.
    /// </summary>
    public void RecordMoment<T>(ResourceRef<T> reference, Identification moment)
    {
        var node = NodeFor(reference.Resource, reference.Epoch);
        if (node.IsBaseEpoch)
        {
            throw new InvalidOperationException("Cannot mark the base epoch: it has no producing increment.");
        }

        node.Mark(moment);
    }

    /// <summary>
    /// Records that a consume node references a moment by its id (without yet knowing which increment
    /// marks it — that binding happens in the Link phase). A referenced moment with no marked increment
    /// is an UndefinedMoment; the recorded readers name who referenced it.
    /// </summary>
    public void RecordMomentRead(Identification moment, GraphNodeId reader) =>
        _momentReads.Add(new MomentReadEdge(moment, reader));

    /// <summary>
    /// Mints a reference to an already-recorded node by its <paramref name="nodeId"/> — the bridge for
    /// fetching a resource the plan bound elsewhere (e.g. a moment's published increment, whose
    /// <see cref="Compile.ResolvedMoment.IncrementNode"/> names the produced node). The reference carries
    /// the node's chain identity and epoch, so resolving it yields that chain's instance. The node must
    /// exist and carry resource type <typeparamref name="T"/>; the reference is ledger-minted, so it
    /// resolves like any other.
    /// </summary>
    public ResourceRef<T> ReferenceTo<T>(GraphNodeId nodeId)
    {
        foreach (var node in _nodes)
        {
            if (node.Id != nodeId)
            {
                continue;
            }

            if (node.ResourceType != typeof(T))
            {
                throw new InvalidOperationException(
                    $"Node {nodeId} carries resource type {node.ResourceType.Name}, not {typeof(T).Name}.");
            }

            return new ResourceRef<T>(node.Resource, node.Epoch);
        }

        throw new InvalidOperationException($"No ledger node {nodeId} — cannot mint a reference to it.");
    }

    /// <summary>The epoch chain (base → produced epochs in order) for one resource.</summary>
    public IReadOnlyList<LedgerNode> ChainFor(GraphNodeId resource) =>
        _chainsByResource.TryGetValue(resource, out var chain) ? chain : [];

    /// <summary>All resource chains keyed by their declaring resource node.</summary>
    public IReadOnlyDictionary<GraphNodeId, List<LedgerNode>> Chains => _chainsByResource;

    private GraphNodeId MintNodeId() => GraphNodeId.Mint(_nodeOrdinal++);

    private LedgerNode NodeFor(GraphNodeId resource, Epoch epoch)
    {
        if (!_chainsByResource.TryGetValue(resource, out var chain))
        {
            throw new InvalidOperationException($"Reference points at an unrecognized resource {resource}.");
        }

        foreach (var node in chain)
        {
            if (node.Epoch == epoch)
            {
                return node;
            }
        }

        throw new InvalidOperationException($"Reference points at an unminted epoch {epoch} of {resource}.");
    }
}

/// <summary>A recorded Read: <paramref name="Reader"/> consumes the resource at <paramref name="Epoch"/>.</summary>
[PublicAPI]
public readonly record struct ReadEdge(GraphNodeId EpochNode, GraphNodeId Reader, GraphNodeId Resource, Epoch Epoch);

/// <summary>A recorded Increment advancing a resource from <paramref name="FromEpoch"/> to <paramref name="ToEpoch"/>.</summary>
[PublicAPI]
public readonly record struct IncrementEdge(
    GraphNodeId SourceNode,
    GraphNodeId ProducedNode,
    GraphNodeId Resource,
    Epoch FromEpoch,
    Epoch ToEpoch);

/// <summary>A recorded moment reference: <paramref name="Reader"/> references the moment <paramref name="Moment"/>.</summary>
[PublicAPI]
public readonly record struct MomentReadEdge(Identification Moment, GraphNodeId Reader);
