using Sparkitect.Modding;

using JetBrains.Annotations;

namespace Sparkitect.GameState;

/// <summary>
/// Capability contract for a state module — a reusable unit of functionality composed into states.
/// Declares the module's <em>direct</em> requirements as instance members; the engine resolves the
/// transitive closure at compose time. The <see cref="TransitiveStateModule"/> base is the default
/// authoring path; implementing this interface directly is the manual / deep-modding escape hatch.
/// </summary>
/// <remarks>
/// Concrete registered modules still declare <see cref="IHasIdentification"/> explicitly — identity is
/// never absorbed by this contract or its bases.
/// </remarks>
[PublicAPI]
public interface IStateModule
{
    /// <summary>
    /// Gets the modules this module <em>directly</em> depends on. The engine resolves the transitive
    /// closure over these entries at compose time; authors list only immediate requirements. Core is
    /// ambient and is never declared here.
    /// </summary>
    IReadOnlyList<Identification> Requires { get; }

    /// <summary>
    /// Gets the activation targets that opt this module into a state automatically. When every target is
    /// present in a composed state, this module (and its <see cref="Requires"/> closure) auto-composes
    /// into that state. Empty by default — activation is opt-in.
    /// </summary>
    IReadOnlyList<Identification> ActivatesWith { get; }
}
