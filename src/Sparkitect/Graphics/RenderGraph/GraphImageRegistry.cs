using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Value registry for shared graph images. A registration binds an
/// <see cref="Identification"/> to a single shared backing described by an
/// <see cref="ImageDescription"/>. Writes into the module-level
/// <see cref="IResourceRegistrationStore"/> rather than a graph-local manager, so the
/// per-graph child container can drain registrations at Setup via the parent chain.
/// </summary>
[Registry(Identifier = "graph_image")]
[PublicAPI]
public partial class GraphImageRegistry(IResourceRegistrationStore store) : IRegistry
{
    [RegistryMethod]
    public void RegisterSharedImage(Identification id, ImageDescription description)
    {
        store.RegisterImage(id, description);
    }

    public void Unregister(Identification id)
    {
        store.UnregisterImage(id);
    }

    public static string Identifier => "graph_image";
}
