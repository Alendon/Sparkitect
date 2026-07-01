using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Stock render-graph registry. Concrete <see cref="IRenderGraph"/> implementations register
/// via the generator-emitted <c>[RenderGraphDeprecatedRegistry.RegisterRenderGraph(...)]</c> attribute;
/// the keyed-factory marker drives generation of the construction-side configurator consumed
/// by <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>. Each
/// registration forwards the id into <see cref="IRenderGraphManagerRegistryFacade"/>.
/// </summary>
[Registry(Identifier = "render_graph_deprecated")]
[PublicAPI]
public partial class RenderGraphDeprecatedRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry<RenderGraphDeprecatedModule>
{
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IRenderGraph>]
    public void RegisterRenderGraph<TGraph>(Identification id)
        where TGraph : class, IRenderGraph, IHasIdentification
    {
        managerFacade.AddRenderGraphType(id);
    }

    public static string Identifier => "render_graph_deprecated";

    public void Unregister(Identification id)
    {
    }
}
