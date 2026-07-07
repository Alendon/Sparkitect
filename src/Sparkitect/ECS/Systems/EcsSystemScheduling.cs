using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Scheduling metadata for a single ECS system: its owning group and the ordering constraints
/// resolved from its <c>OrderAfter</c>/<c>OrderBefore</c> attributes.
/// </summary>
[PublicAPI]
public sealed class EcsSystemScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <summary>Deferred reference to the identification of the group that owns this system.</summary>
    public ILazyIdentification OwnerId { get; set; } = null!;

    /// <summary>Constraints ordering this system after other siblings.</summary>
    public IReadOnlyList<OrderAfterAttribute> OrderAfter => _orderAfter;

    /// <summary>Constraints ordering this system before other siblings.</summary>
    public IReadOnlyList<OrderBeforeAttribute> OrderBefore => _orderBefore;

    /// <summary>Creates the scheduling metadata from the system's ordering attributes.</summary>
    public EcsSystemScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }
}
