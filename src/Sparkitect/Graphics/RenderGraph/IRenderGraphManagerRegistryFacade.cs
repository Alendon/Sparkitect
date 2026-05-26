using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Registry-context facade aggregating pass, resource, and render-graph-type tracking.
/// All three render-graph registries take this single facade in their primary ctor.
/// </summary>
[FacadeFor<IRenderGraphManager>]
[PublicAPI]
public interface IRenderGraphManagerRegistryFacade
{
    /// <summary>Track <paramref name="id"/> as a known pass.</summary>
    void AddPass(Identification id);

    /// <summary>Track <paramref name="id"/> as a known graph resource.</summary>
    void AddResource(Identification id);

    /// <summary>Track <paramref name="id"/> as a known render-graph type.</summary>
    void AddRenderGraphType(Identification id);
}
