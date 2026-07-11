using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.Input;

/// <summary>
/// Registration-facing facade of the input implementation, called only by
/// <see cref="ActionRegistry"/>: registering an action informs the implementation so it wires the
/// action's default binding live from its already-declared setting. Core passes the binding value
/// type opaquely — the implementation owns its interpretation.
/// </summary>
[PublicAPI]
[FacadeFor<IInputActions>]
public interface IInputActionsRegistryFacade
{
    /// <summary>
    /// Informs the implementation of a registered action so it attaches the action's default
    /// binding, built from the setting already declared under <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TResult">The action's result type.</typeparam>
    /// <typeparam name="TDefaultBindingValue">The default binding's settings value type.</typeparam>
    /// <param name="id">The action identification.</param>
    void RegisterAction<TResult, TDefaultBindingValue>(Identification id);

    /// <summary>Detaches the action's live default binding. Called by the action registry.</summary>
    /// <param name="id">The action identification.</param>
    void Unregister(Identification id);
}
