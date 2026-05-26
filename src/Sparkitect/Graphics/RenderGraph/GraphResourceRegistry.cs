using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Stock graph-resource registry. Concrete resource view and physical resource types
/// register via the generator-emitted <c>[GraphResourceRegistry.RegisterResource(...)]</c>
/// attribute. Identification-only — no keyed factory; resource types are instantiated
/// through their bound manager, not via a registry-driven factory. Each registration
/// forwards the id into <see cref="IRenderGraphManagerRegistryFacade"/> for tracking.
/// </summary>
[Registry(Identifier = "graph_resource")]
[PublicAPI]
public partial class GraphResourceRegistry(IRenderGraphManagerRegistryFacade managerFacade) : IRegistry
{
    [RegistryMethod]
    public void RegisterResource<TResource>(Identification id)
        where TResource : class, IHasIdentification
    {
        managerFacade.AddResource(id);
    }

    public static string Identifier => "graph_resource";

    public void Unregister(Identification id)
    {
    }
}
