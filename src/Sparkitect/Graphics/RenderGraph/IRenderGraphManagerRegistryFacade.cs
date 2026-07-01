using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Moments;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Registry-context facade aggregating pass, render-graph-type, and resource-moment tracking; the
/// render-graph registries take this single facade in their primary ctor.
/// </summary>
[FacadeFor<IRenderGraphManager>]
[PublicAPI]
public interface IRenderGraphManagerRegistryFacade
{
    /// <summary>Track <paramref name="id"/> as a known pass.</summary>
    void AddPass(Identification id);

    /// <summary>Track <paramref name="id"/> as a known render-graph type.</summary>
    void AddRenderGraphType(Identification id);

    /// <summary>Record resource moment <paramref name="id"/> and the <paramref name="definition"/> carrying its resource type.</summary>
    void AddResourceMoment(Identification id, ResourceMomentDefinition definition);

    void AddFact<TFact>() where TFact : IHasIdentification;
}
