using JetBrains.Annotations;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>
/// The concrete <see cref="IInstanceContext"/> for one in-flight frame (N=1). It resolves a reference
/// by building the facts registered for that resource via the originating <see cref="ResourceTransaction"/>,
/// caching the built instance per resource so the same reference yields the same instance within the
/// frame. Construction is dependency-first: a fact's <c>CreateInstance</c> resolves its own
/// sub-references through this same context before composing its instance.
/// </summary>
[PublicAPI]
public sealed class InstanceContext : IInstanceContext
{
    private readonly ResourceTransaction _transaction;
    private readonly IReadOnlyDictionary<Identification, ResolvedMoment>? _resolvedMoments;
    private readonly DeclarationLedger? _ledger;
    private readonly Dictionary<GraphNodeId, object?> _instancesByResource = [];

    /// <summary>
    /// Creates an instance context resolving against <paramref name="transaction"/>'s facts. Supplying the
    /// plan's <paramref name="resolvedMoments"/> and the <paramref name="ledger"/> enables
    /// <see cref="ResolveMoment{T}"/>; without them, moment resolution is unavailable.
    /// </summary>
    public InstanceContext(
        ResourceTransaction transaction,
        IReadOnlyDictionary<Identification, ResolvedMoment>? resolvedMoments = null,
        DeclarationLedger? ledger = null)
    {
        _transaction = transaction;
        _resolvedMoments = resolvedMoments;
        _ledger = ledger;
    }

    /// <inheritdoc/>
    public T Resolve<T>(ResourceRef<T> reference)
    {
        if (_instancesByResource.TryGetValue(reference.Resource, out var cached))
        {
            return (T)cached!;
        }

        var facts = _transaction.FactsFor(reference)
            ?? throw new InvalidOperationException(
                $"No declared facts registered for the resource {reference.Resource}.");

        var instance = facts.CreateInstance(this);
        _instancesByResource[reference.Resource] = instance;
        return instance;
    }

    /// <inheritdoc/>
    public T ResolveMoment<T>(Identification moment)
    {
        if (_resolvedMoments is null || _ledger is null)
        {
            throw new InvalidOperationException(
                "InstanceContext.ResolveMoment: moment resolution requires the resolved plan's moments and " +
                "the declaration ledger, but this context was constructed without them.");
        }

        if (!_resolvedMoments.TryGetValue(moment, out var resolved))
        {
            throw new InvalidOperationException(
                $"InstanceContext.ResolveMoment: the moment {moment} was not published by the plan — no " +
                "increment marks it, so there is no backing to resolve.");
        }

        var reference = _ledger.ReferenceTo<T>(resolved.IncrementNode);
        return Resolve(reference);
    }
}
