using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Stock render-graph registry. Concrete <see cref="IRenderGraph"/> implementations register via the
/// generator-emitted <c>[RenderGraphRegistry.RegisterRenderGraph(...)]</c> attribute.
/// </summary>
[Registry(Identifier = "render_graph")]
[PublicAPI]
public partial class RenderGraphRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry
{
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IRenderGraph>]
    public void RegisterRenderGraph<TGraph>(Identification id)
        where TGraph : class, IRenderGraph, IHasIdentification
    {
        managerFacade.AddRenderGraphType(id);
    }

    public static string Identifier => "render_graph";

    public void Unregister(Identification id)
    {
    }
}
