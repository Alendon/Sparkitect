using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Scheduling for functions that execute once when module/state is created.
/// </summary>
public sealed class OnCreateScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly SchedulingParameterAttribute[] _orderingAttributes;

    public OnCreateScheduling(params SchedulingParameterAttribute[] orderingAttributes)
    {
        _orderingAttributes = orderingAttributes;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        builder.AddNode(functionId);

        foreach (var attr in _orderingAttributes)
        {
            // TODO: Process ordering constraints
        }
    }
}

/// <summary>
/// Scheduling for functions that execute once when module/state is destroyed.
/// </summary>
public sealed class OnDestroyScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly SchedulingParameterAttribute[] _orderingAttributes;

    public OnDestroyScheduling(params SchedulingParameterAttribute[] orderingAttributes)
    {
        _orderingAttributes = orderingAttributes;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        builder.AddNode(functionId);

        foreach (var attr in _orderingAttributes)
        {
            // TODO: Process ordering constraints
        }
    }
}

/// <summary>
/// Scheduling for functions that execute when state becomes active leaf.
/// </summary>
public sealed class OnFrameEnterScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly SchedulingParameterAttribute[] _orderingAttributes;

    public OnFrameEnterScheduling(params SchedulingParameterAttribute[] orderingAttributes)
    {
        _orderingAttributes = orderingAttributes;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        builder.AddNode(functionId);

        foreach (var attr in _orderingAttributes)
        {
            // TODO: Process ordering constraints
        }
    }
}

/// <summary>
/// Scheduling for functions that execute when state stops being active leaf.
/// </summary>
public sealed class OnFrameExitScheduling : IScheduling<TransitionFunctionAttribute, TransitionContext, TransitionRegistry>
{
    private readonly SchedulingParameterAttribute[] _orderingAttributes;

    public OnFrameExitScheduling(params SchedulingParameterAttribute[] orderingAttributes)
    {
        _orderingAttributes = orderingAttributes;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, TransitionContext context, Identification functionId)
    {
        builder.AddNode(functionId);

        foreach (var attr in _orderingAttributes)
        {
            // TODO: Process ordering constraints
        }
    }
}
