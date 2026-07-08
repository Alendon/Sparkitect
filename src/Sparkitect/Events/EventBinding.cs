using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.Events;

internal readonly record struct SubscriptionToken(Identification EventId, int SlotIndex, int Generation);

/// <summary>
/// Subscription handle returned by <see cref="IEventManager.Subscribe{TPayload}"/>. Disposing
/// unsubscribes the handler via generation-verified direct-index removal.
/// </summary>
[PublicAPI]
public readonly struct EventBinding : IDisposable
{
    private readonly EventManager? _manager;
    private readonly SubscriptionToken _token;

    internal EventBinding(EventManager manager, SubscriptionToken token)
    {
        _manager = manager;
        _token = token;
    }

    /// <inheritdoc />
    public void Dispose() => _manager?.ReleaseSubscription(_token);
}
