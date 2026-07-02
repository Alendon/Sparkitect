using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attributes =====

/// <summary>
/// Execute once when the module/state is first created.
/// </summary>
[PublicAPI]
public sealed class OnCreateSchedulingAttribute : SchedulingAttribute<OnCreateScheduling>;

/// <summary>
/// Execute once when the module/state is destroyed.
/// </summary>
[PublicAPI]
public sealed class OnDestroySchedulingAttribute : SchedulingAttribute<OnDestroyScheduling>;

/// <summary>
/// Execute when the state becomes the active leaf.
/// </summary>
[PublicAPI]
public sealed class OnFrameEnterSchedulingAttribute : SchedulingAttribute<OnFrameEnterScheduling>;

/// <summary>
/// Execute when the state stops being the active leaf.
/// </summary>
[PublicAPI]
public sealed class OnFrameExitSchedulingAttribute : SchedulingAttribute<OnFrameExitScheduling>;

// ===== Scheduling Implementations =====

/// <summary>
/// Scheduling for functions that execute once when module/state is created.
/// Included when IsEnterTransition AND owner is in DeltaModules.
/// </summary>
[PublicAPI]
public sealed class OnCreateScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <inheritdoc/>
    public Identification OwnerId { get; set; }

    /// <summary>Creates the scheduling with its ordering constraints.</summary>
    /// <param name="orderAfter">Functions this one must run after.</param>
    /// <param name="orderBefore">Functions this one must run before.</param>
    public OnCreateScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>Adds the owning function to the transition graph when the enter transition creates the owner.</summary>
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
[PublicAPI]
public sealed class OnDestroyScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <inheritdoc/>
    public Identification OwnerId { get; set; }

    /// <summary>Creates the scheduling with its ordering constraints.</summary>
    /// <param name="orderAfter">Functions this one must run after.</param>
    /// <param name="orderBefore">Functions this one must run before.</param>
    public OnDestroyScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>Adds the owning function to the transition graph when the exit transition destroys the owner.</summary>
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
[PublicAPI]
public sealed class OnFrameEnterScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <inheritdoc/>
    public Identification OwnerId { get; set; }

    /// <summary>Creates the scheduling with its ordering constraints.</summary>
    /// <param name="orderAfter">Functions this one must run after.</param>
    /// <param name="orderBefore">Functions this one must run before.</param>
    public OnFrameEnterScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>Adds the owning function to the transition graph when its owner becomes the active leaf.</summary>
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
[PublicAPI]
public sealed class OnFrameExitScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <inheritdoc/>
    public Identification OwnerId { get; set; }

    /// <summary>Creates the scheduling with its ordering constraints.</summary>
    /// <param name="orderAfter">Functions this one must run after.</param>
    /// <param name="orderBefore">Functions this one must run before.</param>
    public OnFrameExitScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>Adds the owning function to the transition graph when its owner stops being the active leaf.</summary>
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
