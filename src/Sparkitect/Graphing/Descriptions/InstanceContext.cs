using JetBrains.Annotations;
using Sparkitect.Graphing.Ledger;

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
    private readonly Dictionary<GraphNodeId, object?> _instancesByResource = [];

    /// <summary>Creates an instance context resolving against <paramref name="transaction"/>'s facts.</summary>
    public InstanceContext(ResourceTransaction transaction) => _transaction = transaction;

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
}
