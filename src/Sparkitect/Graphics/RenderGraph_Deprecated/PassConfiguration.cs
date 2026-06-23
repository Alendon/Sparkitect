using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Declarative cross-pass ordering carrier. Harvested from class-level
/// <see cref="OrderAfterAttribute{TOther}"/> / <see cref="OrderBeforeAttribute{TOther}"/> on a pass
/// type into the constructor's attribute arrays, then drained into the render-graph compiler so each
/// declared edge becomes a hard ordering constraint between passes.
/// </summary>
[PublicAPI]
public sealed class PassConfiguration
{
    private readonly OrderAfterAttribute[] _orderAfter;
    private readonly OrderBeforeAttribute[] _orderBefore;

    /// <summary>The pass this configuration belongs to; set before edges are applied.</summary>
    public Identification OwnerId { get; set; }

    public PassConfiguration(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore)
    {
        _orderAfter = orderAfter;
        _orderBefore = orderBefore;
    }

    /// <summary>
    /// Registers the owning pass and applies every declared ordering edge onto the builder, reusing
    /// <see cref="OrderAfterAttribute.Apply"/> / <see cref="OrderBeforeAttribute.Apply"/> verbatim.
    /// </summary>
    public void ApplyEdges(IExecutionGraphBuilder builder)
    {
        builder.AddNode(OwnerId);

        foreach (var after in _orderAfter)
            after.Apply(builder, OwnerId);

        foreach (var before in _orderBefore)
            before.Apply(builder, OwnerId);
    }
}
