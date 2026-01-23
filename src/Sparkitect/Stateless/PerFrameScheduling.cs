using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Scheduling implementation for per-frame functions.
/// Adds the function node and processes ordering constraints from constructor params.
/// </summary>
public sealed class PerFrameScheduling : IScheduling<PerFrameFunctionAttribute, PerFrameContext, PerFrameRegistry>
{
    private readonly SchedulingParameterAttribute[] _orderingAttributes;

    public PerFrameScheduling(params SchedulingParameterAttribute[] orderingAttributes)
    {
        _orderingAttributes = orderingAttributes;
    }

    public void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context, Identification functionId)
    {
        builder.AddNode(functionId);

        foreach (var attr in _orderingAttributes)
        {
            ProcessOrderingAttribute(builder, functionId, attr);
        }
    }

    private static void ProcessOrderingAttribute(IExecutionGraphBuilder builder, Identification functionId, SchedulingParameterAttribute attr)
    {
        // TODO: Resolve target function ID from attribute.
        // For same-scope: OrderBeforeAttribute/OrderAfterAttribute with targetIdentifier
        // For cross-scope: OrderBeforeAttribute<T>/OrderAfterAttribute<T> with TOwner.Identification + targetIdentifier
        // Edge direction: OrderBefore means functionId -> targetId, OrderAfter means targetId -> functionId
    }
}
