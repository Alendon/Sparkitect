using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>Stock declared-fact registry. Mods register concrete <see cref="DeclaredFact"/> types via the generator-emitted <c>[FactRegistry.Register(...)]</c> attribute.</summary>
[Registry(Identifier = "render_graph_fact")]
public partial class FactRegistry : IRegistry<RenderGraphModule>
{
    /// <summary>DI-injected registry-to-manager facade; set by the generated factory, never by mod code.</summary>
    public required IRenderGraphManagerRegistryFacade _manager { private get; init; }

    /// <summary>Registers fact type <typeparamref name="TFact"/> under <paramref name="id"/>; called by generated code from the <c>[Register]</c> attribute, not directly.</summary>
    [RegistryMethod]
    [KeyedFactoryGenerationMarker<DeclaredFact>]
    public void Register<TFact>(Identification id) where TFact : class, DeclaredFact,  IHasIdentification
    {
        _manager.AddFact<TFact>();
    }

    /// <summary>Removes the fact registered under <paramref name="id"/>. No-op: facts are not unregistered at runtime.</summary>
    public void Unregister(Identification id)
    {

    }

    /// <summary>The registry's stable identifier.</summary>
    public static string Identifier => "render_graph_fact";
}