using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

public sealed class EcsSystemScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }
    public IReadOnlyList<OrderAfterAttribute> OrderAfter => _orderAfter;
    public IReadOnlyList<OrderBeforeAttribute> OrderBefore => _orderBefore;

    public EcsSystemScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }
}
