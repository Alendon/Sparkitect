using JetBrains.Annotations;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The concrete <see cref="IResourceTransaction"/> over a <see cref="DeclarationLedger"/>. Each verb
/// delegates to the ledger's mint/record surface; sub-declaration recursively runs the sub-description
/// inside this same transaction and registers its facts so the instance context can resolve the
/// dependency later. Enforces the one-declaration-per-instance rule by tracking declared description
/// instances and rejecting reuse with the <see cref="CompileError.DescriptionReuse"/> case. Epoch
/// positions are never resolved here — resolution is the Link phase's job.
/// </summary>
[PublicAPI]
public sealed class ResourceTransaction : IResourceTransaction
{
    private readonly DeclarationLedger _ledger;
    private readonly Dictionary<GraphNodeId, object> _factsByResource = [];
    private readonly HashSet<object> _declaredInstances = new(ReferenceEqualityComparer.Instance);
    private readonly Stack<GraphNodeId> _selfResources = [];

    /// <summary>Creates a transaction recording into <paramref name="ledger"/>.</summary>
    public ResourceTransaction(DeclarationLedger ledger) => _ledger = ledger;

    /// <inheritdoc/>
    public void Read<T>(ResourceRef<T> reference) =>
        _ledger.RecordRead(reference, reference.Resource);

    /// <inheritdoc/>
    public ResourceRef<T> Increment<T>(ResourceRef<T> reference) =>
        _ledger.RecordIncrement(reference, Identification.Empty);

    /// <inheritdoc/>
    public ResourceRef<T> Increment<T>(ResourceRef<T> reference, Identification moment)
    {
        var produced = _ledger.RecordIncrement(reference, Identification.Empty);
        _ledger.RecordMoment(produced, moment);
        return produced;
    }

    /// <inheritdoc/>
    public void ReferenceMoment(Identification moment) => _ledger.RecordMomentRead(moment, CurrentSelf());

    /// <inheritdoc/>
    public ResourceRef<TSub> Declare<TSub>(IResourceDescription<TSub> description)
    {
        if (!_declaredInstances.Add(description))
        {
            throw new DescriptionReuseException(new CompileError.DescriptionReuse(LastResource()));
        }

        var resourceRef = _ledger.Declare<TSub>(Identification.Empty);
        _selfResources.Push(resourceRef.Resource);
        DeclaredFacts<TSub> facts;
        try
        {
            facts = description.Declare(this);
        }
        finally
        {
            _selfResources.Pop();
        }

        _factsByResource[resourceRef.Resource] = facts;
        return resourceRef;
    }

    /// <inheritdoc/>
    public ResourceRef<T> Self<T>()
    {
        if (_selfResources.Count == 0)
        {
            throw new InvalidOperationException("Self() is only valid inside a description's Declare.");
        }

        return new ResourceRef<T>(_selfResources.Peek(), Epoch.Base);
    }

    /// <summary>
    /// The facts registered for the resource <paramref name="reference"/> points at, used by the
    /// instance context to build the dependency. Null when no facts were registered (a reference
    /// minted by an increment shares its source resource's facts).
    /// </summary>
    internal DeclaredFacts<T>? FactsFor<T>(ResourceRef<T> reference) =>
        _factsByResource.TryGetValue(reference.Resource, out var facts)
            ? (DeclaredFacts<T>)facts
            : null;

    private GraphNodeId LastResource() =>
        _ledger.Nodes.Count > 0 ? _ledger.Nodes[^1].Resource : GraphNodeId.None;

    private GraphNodeId CurrentSelf()
    {
        if (_selfResources.Count == 0)
        {
            throw new InvalidOperationException("ReferenceMoment is only valid inside a description's Declare.");
        }

        return _selfResources.Peek();
    }
}

/// <summary>
/// Thrown when the same description instance is declared more than once, carrying the
/// <see cref="CompileError.DescriptionReuse"/> case as the structured diagnostic.
/// </summary>
[PublicAPI]
public sealed class DescriptionReuseException(CompileError.DescriptionReuse error)
    : InvalidOperationException("A description instance was declared more than once.")
{
    /// <summary>The structured reuse diagnostic naming the reused declaration node.</summary>
    public CompileError.DescriptionReuse Error { get; } = error;
}
