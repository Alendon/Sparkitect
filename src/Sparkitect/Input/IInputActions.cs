using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Input;

/// <summary>
/// The implementation-neutral resolve hub every input implementation provides. Push is the
/// normal event-first consumption path; pull is the deliberate secondary alternative. This
/// surface names no device vocabulary and exposes no event-bus type — implementations own all
/// sampling, storage, dispatch, and lifetime behind it.
/// </summary>
[PublicAPI]
[RegistryFacade<IInputActionsRegistryFacade>]
public interface IInputActions
{
    /// <summary>
    /// Resolves <paramref name="id"/> to a push binding: <paramref name="callback"/> is invoked
    /// for every already-processed <c>Value(T)</c> the action produces, including an identical
    /// value on consecutive frames; <c>NoValue</c> is never delivered.
    /// </summary>
    /// <typeparam name="T">The action's result type.</typeparam>
    /// <param name="id">The action's typed identification.</param>
    /// <param name="callback">Invoked with each actively-contributing result.</param>
    /// <returns>The live push binding; disposing stops the callback.</returns>
    IPushBinding Push<T>(Identification<T> id, Action<T> callback);

    /// <summary>
    /// Resolves <paramref name="id"/> to a pull binding for explicit, on-demand reads of the
    /// action's already-processed result.
    /// </summary>
    /// <typeparam name="T">The action's result type.</typeparam>
    /// <param name="id">The action's typed identification.</param>
    /// <returns>The live pull binding; disposing invalidates further reads.</returns>
    IPullBinding<T> Pull<T>(Identification<T> id);
}
