using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Stock render-graph registry. Concrete <see cref="IRenderGraph"/> implementations register via the
/// generator-emitted <c>[RenderGraphRegistry.RegisterRenderGraph(...)]</c> attribute.
/// </summary>
[Registry(Identifier = "render_graph")]
[PublicAPI]
public partial class RenderGraphRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry<RenderGraphModule>
{
    /// <summary>Registers render-graph type <typeparamref name="TGraph"/> under <paramref name="id"/>; called by generated code from the <c>[RegisterRenderGraph]</c> attribute, not directly.</summary>
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IRenderGraph>]
    public void RegisterRenderGraph<TGraph>(Identification id)
        where TGraph : class, IRenderGraph, IHasIdentification
    {
        managerFacade.AddRenderGraphType(id);
    }

    /// <summary>The registry's stable identifier.</summary>
    public static string Identifier => "render_graph";

    /// <summary>Removes the render-graph type registered under <paramref name="id"/>. No-op: graph types are not unregistered at runtime.</summary>
    public void Unregister(Identification id)
    {
    }
}
