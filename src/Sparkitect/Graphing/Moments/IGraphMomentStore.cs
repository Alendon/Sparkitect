using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Graphing.Moments;

/// <summary>
/// The home for moment registrations keyed by <see cref="Identification"/>. A registration records the
/// moment's resource type (carried by a <see cref="MomentDefinition"/>) and nothing else — a moment
/// declares name + resource type only, never backing, position, or producer. The link stage queries the
/// store to learn the resource type a referenced moment carries.
/// </summary>
[PublicAPI]
public interface IGraphMomentStore
{
    /// <summary>
    /// Records a moment registration. Keyed by <paramref name="id"/>; a later registration of the same
    /// id overwrites the prior one (last-writer wins).
    /// </summary>
    void RegisterMoment(Identification id, MomentDefinition definition);

    /// <summary>
    /// Resolves a previously registered moment definition by its identification. Returns false when the
    /// id is unregistered.
    /// </summary>
    bool TryGetMoment(Identification id, out MomentDefinition definition);

    /// <summary>Removes a previously registered moment. No-op when the id is unregistered.</summary>
    void UnregisterMoment(Identification id);

    /// <summary>All currently registered moment definitions, keyed by identification.</summary>
    IReadOnlyDictionary<Identification, MomentDefinition> RegisteredMoments { get; }
}
