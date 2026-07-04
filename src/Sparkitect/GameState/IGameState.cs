using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Capability contract for a game state — a specific runtime configuration composed from modules.
/// States form a parent-child hierarchy and declare their <em>direct</em> module set as instance members;
/// the engine resolves the transitive closure and inherited modules at compose time. The
/// <see cref="TransitiveGameState"/> base is the default authoring path; implementing this interface
/// directly is the manual / deep-modding escape hatch.
/// </summary>
/// <remarks>
/// Concrete registered states still declare <see cref="IHasIdentification"/> explicitly — identity is
/// never absorbed by this contract or its bases.
/// </remarks>
[PublicAPI]
public interface IGameState
{
    /// <summary>
    /// Gets the identification of the parent state. States can only transition to their immediate parent
    /// or children. The root state's parent is <see cref="Identification.Empty"/>.
    /// </summary>
    Identification ParentId { get; }

    /// <summary>
    /// Gets the modules this state introduces <em>directly</em> (its own contribution, independent of the
    /// parent chain). Inherited modules and the transitive closure over module requirements are resolved
    /// by the engine at compose time.
    /// </summary>
    IReadOnlyList<Identification> DirectModules { get; }
}
