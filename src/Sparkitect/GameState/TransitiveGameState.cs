using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Default authoring base for game states. Authors declare only their <see cref="ParentId"/> and their
/// <em>direct</em> module contribution via <see cref="DirectModules"/>; the engine resolves inherited
/// modules and the transitive closure at compose time. Implement <see cref="IGameState"/> directly only
/// for the manual / deep-modding escape hatch.
/// </summary>
/// <remarks>
/// The implicit parameterless constructor stays accessible so the registration path can construct an
/// ephemeral instance (via a <c>new()</c> constraint) to read the state's direct declarations and discard
/// it — no reflection. This base intentionally does NOT absorb the identity contract; concrete states
/// keep declaring their identification explicitly.
/// </remarks>
[PublicAPI]
public abstract class TransitiveGameState : IGameState
{
    /// <summary>
    /// Gets the identification of the parent state. Override to declare the immediate parent; the root
    /// state's parent is <see cref="Identification.Empty"/>.
    /// </summary>
    public abstract Identification ParentId { get; }

    /// <summary>
    /// Gets the modules this state introduces <em>directly</em>. Override to declare this state's own
    /// contribution only; inherited modules and the transitive closure are resolved by the engine.
    /// </summary>
    public abstract IReadOnlyList<Identification> DirectModules { get; }
}
