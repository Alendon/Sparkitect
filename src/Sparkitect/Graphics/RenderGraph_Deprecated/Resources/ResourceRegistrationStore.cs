using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>
/// The single module-level resource-registration store. Bound to
/// <see cref="RenderGraphDeprecatedModule"/> so the per-graph child container (which chains to the
/// host) can inject it into graph-local managers. Carries an independent dictionary per
/// resource family (images and buffers); further families get their own dictionary.
/// </summary>
[StateService<IResourceRegistrationStore, RenderGraphDeprecatedModule>]
[PublicAPI]
public sealed class ResourceRegistrationStore : IResourceRegistrationStore
{
    private readonly Dictionary<Identification, ImageDescription> _images = [];
    private readonly Dictionary<Identification, BufferDescription> _buffers = [];

    public void RegisterImage(Identification id, ImageDescription description) =>
        _images[id] = description;

    public bool TryGetImage(Identification id, out ImageDescription description) =>
        _images.TryGetValue(id, out description);

    public void UnregisterImage(Identification id) => _images.Remove(id);

    public IReadOnlyDictionary<Identification, ImageDescription> RegisteredImages => _images;

    public void RegisterBuffer(Identification id, BufferDescription description) =>
        _buffers[id] = description;

    public bool TryGetBuffer(Identification id, out BufferDescription description) =>
        _buffers.TryGetValue(id, out description);

    public void UnregisterBuffer(Identification id) => _buffers.Remove(id);

    public IReadOnlyDictionary<Identification, BufferDescription> RegisteredBuffers => _buffers;
}
