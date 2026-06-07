using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// The single module-level resource-registration store. Bound to
/// <see cref="RenderGraphModule"/> so the per-graph child container (which chains to the
/// host) can inject it into graph-local managers. Image registrations only this phase;
/// keep the backing single-dictionary until a second resource family lands.
/// </summary>
[StateService<IResourceRegistrationStore, RenderGraphModule>]
[PublicAPI]
public sealed class ResourceRegistrationStore : IResourceRegistrationStore
{
    private readonly Dictionary<Identification, ImageDescription> _images = [];

    public void RegisterImage(Identification id, ImageDescription description) =>
        _images[id] = description;

    public bool TryGetImage(Identification id, out ImageDescription description) =>
        _images.TryGetValue(id, out description);

    public void UnregisterImage(Identification id) => _images.Remove(id);

    public IReadOnlyDictionary<Identification, ImageDescription> RegisteredImages => _images;
}
