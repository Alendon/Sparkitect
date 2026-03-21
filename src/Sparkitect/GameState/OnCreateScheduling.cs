using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attributes =====

/// <summary>
/// Execute once when the module/state is first created.
/// </summary>
public sealed class OnCreateSchedulingAttribute : SchedulingAttribute<OnCreateScheduling>;

/// <summary>
/// Execute once when the module/state is destroyed.
/// </summary>
public sealed class OnDestroySchedulingAttribute : SchedulingAttribute<OnDestroyScheduling>;

/// <summary>
/// Execute when the state becomes the active leaf.
/// </summary>
public sealed class OnFrameEnterSchedulingAttribute : SchedulingAttribute<OnFrameEnterScheduling>;

/// <summary>
/// Execute when the state stops being the active leaf.
/// </summary>
public sealed class OnFrameExitSchedulingAttribute : SchedulingAttribute<OnFrameExitScheduling>;

// ===== Scheduling Implementations =====

/// <summary>
/// Scheduling for functions that execute once when module/state is created.
/// Included when IsEnterTransition AND owner is in DeltaModules.
/// </summary>
public sealed class OnCreateScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public OnCreateScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        if (!context.IsEnterTransition) return;
        if (!context.DeltaModules.Contains(OwnerId) && context.StateStack[^1].StateId != OwnerId) return;

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
public sealed class OnDestroyScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public OnDestroyScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        if (context.IsEnterTransition) return;
        if (!context.DeltaModules.Contains(OwnerId) && context.StateStack[^1].StateId != OwnerId) return;

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
public sealed class OnFrameEnterScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public OnFrameEnterScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        if (!context.IsEnterTransition) return;
        if (!context.IsModuleLoaded(OwnerId) && context.StateStack[^1].StateId != OwnerId) return;

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
public sealed class OnFrameExitScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    public Identification OwnerId { get; set; }

    public OnFrameExitScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        if (context.IsEnterTransition) return;
        if (!context.IsModuleLoaded(OwnerId) && context.StateStack[^1].StateId != OwnerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
