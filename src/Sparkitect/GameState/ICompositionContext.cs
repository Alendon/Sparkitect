using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Engine-provided compose-time context handed to authoring surfaces that resolve their own module set.
/// Deliberately minimal: it exposes only the transitive-closure resolver, intended to back an optional
/// convenience for manual / deep-modding implementers of <see cref="IStateModule"/> and
/// <see cref="IGameState"/>. No settings or configuration surface is exposed (deferred — no consumer).
/// </summary>
[PublicAPI]
public interface ICompositionContext
{
    /// <summary>
    /// Resolves the transitive closure of module requirements from a set of <em>direct</em> requirements.
    /// The returned set contains every entry in <paramref name="directRequirements"/> plus every module
    /// reachable through their <see cref="IStateModule.Requires"/> chains.
    /// </summary>
    /// <param name="directRequirements">The direct (immediate) module requirements to close over.</param>
    /// <returns>The transitively-closed module set.</returns>
    IReadOnlyList<Identification> ResolveClosure(IReadOnlyList<Identification> directRequirements);
}
