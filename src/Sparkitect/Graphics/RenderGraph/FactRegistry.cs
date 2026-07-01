using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

[Registry(Identifier = "render_graph_fact")]
public partial class FactRegistry : IRegistry<RenderGraphModule>
{
    public required IRenderGraphManagerRegistryFacade _manager { private get; init; }

    [RegistryMethod]
    [KeyedFactoryGenerationMarker<DeclaredFact>]
    public void Register<TFact>(Identification id) where TFact : class, DeclaredFact,  IHasIdentification
    {
        _manager.AddFact<TFact>();
    }
    
    public void Unregister(Identification id)
    {
        
    }

    public static string Identifier => "render_graph_fact";
}