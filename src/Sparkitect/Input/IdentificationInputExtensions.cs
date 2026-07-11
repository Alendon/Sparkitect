using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Input;

/// <summary>
/// Ergonomic sugar so a generated <see cref="Identification{T}"/> property resolves push/pull
/// bindings without repeating the result type at the call site. Ownership is unchanged: the
/// implementation behind <c>actions</c> still owns storage, dispatch, and lifetime.
/// </summary>
[PublicAPI]
public static class IdentificationInputExtensions
{
    /// <summary>Resolves <paramref name="id"/> to a push binding through <paramref name="actions"/>.</summary>
    /// <typeparam name="T">The action's result type.</typeparam>
    /// <param name="id">The action's typed identification.</param>
    /// <param name="actions">The resolve hub to push through.</param>
    /// <param name="callback">Invoked with each actively-contributing result.</param>
    public static IPushBinding Push<T>(this Identification<T> id, IInputActions actions, Action<T> callback) =>
        actions.Push(id, callback);

    /// <summary>Resolves <paramref name="id"/> to a pull binding through <paramref name="actions"/>.</summary>
    /// <typeparam name="T">The action's result type.</typeparam>
    /// <param name="id">The action's typed identification.</param>
    /// <param name="actions">The resolve hub to pull through.</param>
    public static IPullBinding<T> Pull<T>(this Identification<T> id, IInputActions actions) =>
        actions.Pull(id);
}
