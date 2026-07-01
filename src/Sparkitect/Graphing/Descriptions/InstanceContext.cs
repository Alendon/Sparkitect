using JetBrains.Annotations;
using Sparkitect.Graphing.Compile;
using Sparkitect.Graphing.Ledger;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Descriptions;

/// <summary>The concrete <see cref="IInstanceContext"/> for one in-flight frame (N=1). Resolves a reference by building the resource's registered facts, caching per resource so the same reference yields the same instance. Construction is dependency-first.</summary>
[PublicAPI]
public sealed class InstanceContext : IInstanceContext, IDisposable
{
    private readonly ResourceTransaction _transaction;
    private readonly IReadOnlyDictionary<Identification, ResolvedMoment>? _resolvedMoments;
    private readonly DeclarationLedger? _ledger;
    private readonly Dictionary<GraphNodeId, object?> _instancesByResource = [];
    // Built instances paired with the fact that made them, in build order, so teardown can honour each
    // fact's CleanupStrategy (dependents dispose before dependencies).
    private readonly List<(object? Instance, DeclaredFact Fact)> _built = [];
    private bool _disposed;

    /// <summary>Creates a context resolving against <paramref name="transaction"/>'s facts. Supplying <paramref name="resolvedMoments"/> and <paramref name="ledger"/> enables <see cref="ResolveMoment{T}"/>; without them, moment resolution is unavailable.</summary>
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
        _built.Add((instance, facts));
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

    /// <summary>Releases this frame's built instances by honouring each fact's <see cref="CleanupStrategy"/>.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (var i = _built.Count - 1; i >= 0; i--)
        {
            var (instance, fact) = _built[i];
            if (fact.CleanupStrategy is CleanupStrategy.Dispose)
                (instance as IDisposable)?.Dispose();
        }

        _built.Clear();
    }
}
