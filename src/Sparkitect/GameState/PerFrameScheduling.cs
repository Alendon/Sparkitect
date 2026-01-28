using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attribute =====

/// <summary>
/// Default scheduling for PerFrame functions. Functions execute in dependency order every frame.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PerFrameSchedulingAttribute
    : SchedulingAttribute<PerFrameScheduling, PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>;

// ===== Scheduling Implementation =====

/// <summary>
/// Scheduling implementation for per-frame functions.
/// Included when owner module is loaded in state stack.
/// </summary>
public sealed class PerFrameScheduling : IScheduling<PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public PerFrameScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context, Identification functionId, Identification ownerId)
    {
        if (!context.IsModuleLoaded(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
