using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Moments;

/// <summary>
/// Default in-memory <see cref="IGraphMomentStore"/>: a last-writer-wins map of moment identification to
/// its resource-type-carrying <see cref="MomentDefinition"/>.
/// </summary>
[PublicAPI]
public sealed class GraphMomentStore : IGraphMomentStore
{
    private readonly Dictionary<Identification, MomentDefinition> _moments = [];

    /// <inheritdoc/>
    public void RegisterMoment(Identification id, MomentDefinition definition) => _moments[id] = definition;

    /// <inheritdoc/>
    public bool TryGetMoment(Identification id, out MomentDefinition definition) =>
        _moments.TryGetValue(id, out definition!);

    /// <inheritdoc/>
    public void UnregisterMoment(Identification id) => _moments.Remove(id);

    /// <inheritdoc/>
    public IReadOnlyDictionary<Identification, MomentDefinition> RegisteredMoments => _moments;
}
