using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

// ===== Scheduling Attribute =====

/// <summary>
/// Default scheduling for PerFrame functions. Functions execute in dependency order every frame.
/// </summary>
[PublicAPI]
public sealed class PerFrameSchedulingAttribute : SchedulingAttribute<PerFrameScheduling>;

// ===== Scheduling Implementation =====

/// <summary>
/// Scheduling implementation for per-frame functions.
/// Included when owner module is loaded in state stack.
/// </summary>
[PublicAPI]
public sealed class PerFrameScheduling : IScheduling
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <inheritdoc/>
    public ILazyIdentification OwnerId { get; set; } = null!;

    /// <summary>Creates the scheduling with its ordering constraints.</summary>
    /// <param name="orderAfter">Functions this one must run after.</param>
    /// <param name="orderBefore">Functions this one must run before.</param>
    public PerFrameScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>Adds the owning function to the per-frame graph when its owner is loaded in the state stack.</summary>
    public void BuildGraph(IExecutionGraphBuilder builder, PerFrameContext context, Identification functionId)
    {
        var ownerId = OwnerId.Resolve();
        if (!context.IsModuleLoaded(ownerId) && context.StateStack[^1].StateId != ownerId) return;

        builder.AddNode(functionId);

        foreach (var after in _orderAfter)
            after.Apply(builder, functionId);

        foreach (var before in _orderBefore)
            before.Apply(builder, functionId);
    }
}
