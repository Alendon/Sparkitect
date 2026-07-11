using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Events;

[StateService<IEventManager, EventModule>]
internal sealed class EventManager : IEventManager, IEventManagerRegistryFacade
{
    private readonly Dictionary<Identification, List<(int generation, Action<object?> handler)>> _subscribers = new();
    private int _nextGeneration;
    private readonly Dictionary<Identification, object> _definitions = new();

    public void RegisterEvent<TPayload>(Identification id, IEventDefinition<TPayload> definition) => _definitions[id] = definition;

    public void Unregister(Identification id)
    {
        _definitions.Remove(id);
        _subscribers.Remove(id);
    }

    public EventBinding Subscribe<TPayload>(Identification<TPayload> id, Action<TPayload> handler)
    {
        var bareId = (Identification)id;
        Action<object?> wrapped = obj => handler((TPayload)obj!);

        if (!_subscribers.TryGetValue(bareId, out var list))
        {
            list = [];
            _subscribers[bareId] = list;
        }

        var gen = _nextGeneration++;
        var slotIndex = list.Count;
        list.Add((gen, wrapped));

        return new EventBinding(this, new SubscriptionToken(bareId, slotIndex, gen));
    }

    public void Publish<TPayload>(Identification<TPayload> id, TPayload payload)
    {
        var bareId = (Identification)id;
        if (!_subscribers.TryGetValue(bareId, out var list)) return;

        // Allocation-free hot path (input publishes per action, per frame): tombstoning keeps
        // slot indices stable under unsubscribe-during-publish, and capturing the count excludes
        // handlers subscribed during this publish.
        var count = list.Count;
        for (var i = 0; i < count; i++)
            list[i].handler?.Invoke(payload);
    }

    internal void ReleaseSubscription(SubscriptionToken token)
    {
        if (!_subscribers.TryGetValue(token.EventId, out var list)) return;
        if (token.SlotIndex < 0 || token.SlotIndex >= list.Count) return;
        if (list[token.SlotIndex].generation == token.Generation)
            list[token.SlotIndex] = (-1, null!); // tombstone: keeps later slot indices stable
    }
}
