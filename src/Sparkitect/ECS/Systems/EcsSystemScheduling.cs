using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

public sealed class EcsSystemScheduling
    : IScheduling<EcsSystemFunctionAttribute, EcsSystemContext, SystemRegistry, IEcsGraphBuilder>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public EcsSystemScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IEcsGraphBuilder builder, EcsSystemContext context,
        Identification functionId, Identification ownerId)
    {
        // Inclusion filtering: check if system is active in the World
        var systems = context.World.GetSystems();
        if (!systems.TryGetValue(functionId, out var state) || state != SystemState.Active)
            return;

        // Check group state if ownerId is a registered group
        var groups = context.World.GetSystemGroups();
        if (groups.TryGetValue(ownerId, out var groupState) && groupState != SystemState.Active)
            return;

        builder.AddNode(functionId, ownerId);

        // Manual edge application (OrderAttributes.Apply takes IExecutionGraphBuilder, not IEcsGraphBuilder)
        foreach (var after in _orderAfter)
            builder.AddEdge(after.Other, functionId, after.Optional);

        foreach (var before in _orderBefore)
            builder.AddEdge(functionId, before.Other, before.Optional);
    }
}
