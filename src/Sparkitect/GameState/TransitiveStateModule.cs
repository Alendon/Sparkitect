using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Default authoring base for state modules. Authors declare only their <em>direct</em> requirements via
/// <see cref="Requires"/> (and optionally <see cref="ActivatesWith"/>); the engine resolves the transitive
/// closure at compose time. Implement <see cref="IStateModule"/> directly only for the manual /
/// deep-modding escape hatch.
/// </summary>
/// <remarks>
/// The implicit parameterless constructor stays accessible so the registration path can construct an
/// ephemeral instance (via a <c>new()</c> constraint) to read the module's direct declarations and discard
/// it — no reflection. This base intentionally does NOT absorb the identity contract; concrete modules
/// keep declaring their identification explicitly.
/// </remarks>
[PublicAPI]
public abstract class TransitiveStateModule : IStateModule
{
    /// <summary>
    /// Gets the modules this module <em>directly</em> depends on. Override to declare immediate
    /// requirements only; the engine resolves the transitive closure. Core is ambient — never list it.
    /// </summary>
    public abstract IReadOnlyList<Identification> Requires { get; }

    /// <summary>
    /// Gets the activation targets that opt this module into a state automatically. Empty by default —
    /// override to make this an activation-conditioned integration module (opt-in).
    /// </summary>
    public virtual IReadOnlyList<Identification> ActivatesWith => [];
}
