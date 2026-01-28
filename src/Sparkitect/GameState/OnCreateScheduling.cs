using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attributes =====

/// <summary>
/// Execute once when the module/state is first created.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCreateSchedulingAttribute
    : SchedulingAttribute<OnCreateScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute once when the module/state is destroyed.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnDestroySchedulingAttribute
    : SchedulingAttribute<OnDestroyScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute when the state becomes the active leaf.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameEnterSchedulingAttribute
    : SchedulingAttribute<OnFrameEnterScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

/// <summary>
/// Execute when the state stops being the active leaf.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameExitSchedulingAttribute
    : SchedulingAttribute<OnFrameExitScheduling, TransitionFunctionAttribute, TransitionContext, TransitionRegistry>;

// ===== Scheduling Implementations =====

/// <summary>
/// Scheduling for functions that execute once when module/state is created.
/// Included when IsEnterTransition AND owner is in DeltaModules.
/// </summary>
public sealed class OnCreateScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public OnCreateScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId, Identification ownerId)
    {
        if (!context.IsEnterTransition) return;
        if (!context.DeltaModules.Contains(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}

/// <summary>
/// Scheduling for functions that execute once when module/state is destroyed.
/// Included when !IsEnterTransition AND owner is in DeltaModules.
/// </summary>
public sealed class OnDestroyScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public OnDestroyScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId, Identification ownerId)
    {
        if (context.IsEnterTransition) return;
        if (!context.DeltaModules.Contains(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}

/// <summary>
/// Scheduling for functions that execute when state becomes active leaf.
/// Included when IsEnterTransition AND owner is loaded in state stack.
/// </summary>
public sealed class OnFrameEnterScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public OnFrameEnterScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId, Identification ownerId)
    {
        if (!context.IsEnterTransition) return;
        if (!context.IsModuleLoaded(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}

/// <summary>
/// Scheduling for functions that execute when state stops being active leaf.
/// Included when !IsEnterTransition AND owner is loaded in state stack.
/// </summary>
public sealed class OnFrameExitScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public OnFrameExitScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId, Identification ownerId)
    {
        if (context.IsEnterTransition) return;
        if (!context.IsModuleLoaded(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
