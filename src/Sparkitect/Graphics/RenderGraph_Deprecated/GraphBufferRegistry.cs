using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Value registry for shared graph buffers. A registration binds an
/// <see cref="Identification"/> to a single shared device backing described by a
/// <see cref="BufferDescription"/>. Writes into the module-level
/// <see cref="IResourceRegistrationStore"/> rather than a graph-local manager, so the
/// per-graph child container can drain registrations at Setup via the parent chain.
/// </summary>
[Registry(Identifier = "graph_buffer")]
[PublicAPI]
public partial class GraphBufferRegistry(IResourceRegistrationStore store) : IRegistry
{
    [RegistryMethod]
    public void RegisterSharedBuffer(Identification id, BufferDescription description)
    {
        store.RegisterBuffer(id, description);
    }

    public void Unregister(Identification id)
    {
        store.UnregisterBuffer(id);
    }

    public static string Identifier => "graph_buffer";
}
