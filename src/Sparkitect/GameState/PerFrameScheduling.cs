using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attribute =====

/// <summary>
/// Default scheduling for PerFrame functions. Functions execute in dependency order every frame.
/// </summary>
public sealed class PerFrameSchedulingAttribute : SchedulingAttribute<PerFrameScheduling>;

// ===== Scheduling Implementation =====

/// <summary>
/// Scheduling implementation for per-frame functions.
/// Included when owner module is loaded in state stack.
/// </summary>
public sealed class PerFrameScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public PerFrameScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context, Identification functionId)
    {
        if (!context.IsModuleLoaded(OwnerId) && context.StateStack[^1].StateId != OwnerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
