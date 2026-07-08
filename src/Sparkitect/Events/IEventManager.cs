using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Events;

/// <summary>
/// Runtime event bus surface: synchronous publish/subscribe keyed by typed identifications.
/// </summary>
[PublicAPI]
[RegistryFacade<IEventManagerRegistryFacade>]
public interface IEventManager
{
    /// <summary>Subscribes <paramref name="handler"/> to events published under <paramref name="id"/>.</summary>
    /// <typeparam name="TPayload">The event payload type.</typeparam>
    /// <param name="id">The typed event identification.</param>
    /// <param name="handler">Invoked synchronously with the payload on each publish.</param>
    /// <returns>An <see cref="EventBinding"/> that unsubscribes when disposed.</returns>
    EventBinding Subscribe<TPayload>(Identification<TPayload> id, Action<TPayload> handler);

    /// <summary>Publishes <paramref name="payload"/> to all subscribers of <paramref name="id"/>.</summary>
    /// <typeparam name="TPayload">The event payload type.</typeparam>
    /// <param name="id">The typed event identification.</param>
    /// <param name="payload">The payload delivered to each subscriber.</param>
    void Publish<TPayload>(Identification<TPayload> id, TPayload payload);
}
