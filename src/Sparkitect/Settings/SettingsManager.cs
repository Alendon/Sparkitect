using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;

namespace Sparkitect.Settings;

/// <summary>
/// The settings engine. Resolves effective values over an ordered source list, exposes a Result-based
/// write primitive, and dispatches effective-change callbacks synchronously. Source ordering translates
/// each source's OrderBefore/OrderAfter metadata into the shared ordering core and fails loud on cycles
/// or missing required sources.
/// </summary>
[StateService<ISettingsManager, CoreModule>]
[PublicAPI]
public sealed class SettingsManager : ISettingsManager
{
    private static readonly object DefaultFrameToken = new();

    // Boxed SettingDefinition<T>; the concrete T is recovered at the typed call boundary.
    private readonly Dictionary<Identification, object> _declarations = new();
    private readonly Dictionary<Identification, ISettingSource> _sources = new();
    private readonly Dictionary<Identification, List<Subscription>> _subscriptions = new();

    private List<Identification> _orderedSourceIds = [];
    private bool _orderStale;
    private Func<object> _frameTokenProvider = () => DefaultFrameToken;
    private Identification? _userSourceId;

    /// <inheritdoc/>
    public void Declare<T>(Identification<T> id, SettingDefinition<T> definition) => _declarations[id] = definition;

    /// <inheritdoc/>
    public void Undeclare(Identification id) => _declarations.Remove(id);

    /// <inheritdoc/>
    public ISettingDeclaration? GetDeclaration(Identification id) =>
        _declarations.TryGetValue(id, out var declaration) ? declaration as ISettingDeclaration : null;

    /// <inheritdoc/>
    public void RegisterSource(Identification id, ISettingSource source)
    {
        _sources[id] = source;
        _userSourceId ??= source.CanWrite ? id : null;
        _orderStale = true;
    }

    // Playback half of record-and-playback registration: RegisterSource only records, so a registration
    // pass may record sources whose required order targets register later in the same pass. Invoked by
    // the settings module's frame-enter function and lazily by the first resolve — whichever runs first
    // recomputes (and fail-louds on a bad graph).
    internal void ProcessRegisteredSources()
    {
        if (!_orderStale)
        {
            return;
        }

        RecomputeOrder();
        _orderStale = false;
    }

    /// <inheritdoc/>
    public Setting<T> GetSetting<T>(Identification<T> id) => new(this, id);

    /// <inheritdoc/>
    public T GetValue<T>(Identification<T> id) => Resolve<T>(id);

    /// <inheritdoc/>
    public Result<SetError> Set<T>(Identification<T> id, Identification sourceId, T value)
    {
        if (!_declarations.ContainsKey(id))
        {
            return new SetError.UnknownSetting(id);
        }

        if (!_sources.TryGetValue(sourceId, out var source))
        {
            return new SetError.UnknownSource(sourceId);
        }

        if (!source.CanWrite)
        {
            return new SetError.SourceReadonly(sourceId);
        }

        var before = Resolve<T>(id);
        if (source.Write(id, value) is Result<SetError>.Error error)
        {
            return error.Value;
        }

        var after = Resolve<T>(id);
        if (!EqualityComparer<T>.Default.Equals(before, after))
        {
            Dispatch(id, after);
        }

        return new Result<SetError>.Ok();
    }

    /// <inheritdoc/>
    public Result<SetError> SetUserValue<T>(Identification<T> id, T value) =>
        _userSourceId is { } userSource
            ? Set(id, userSource, value)
            : new SetError.UnknownSource(Identification.Empty);

    /// <inheritdoc/>
    public IDisposable Subscribe<T>(Identification<T> id, Action<T> onEffectiveChange)
    {
        var subscription = new Subscription(_frameTokenProvider(), value => onEffectiveChange((T)value!));
        if (!_subscriptions.TryGetValue(id, out var list))
        {
            list = [];
            _subscriptions[id] = list;
        }

        list.Add(subscription);
        return new Unsubscriber(list, subscription);
    }

    /// <summary>
    /// Sets the frame-token provider used to tag new subscriptions. Plan 04 wires this to the current
    /// state frame's identity; unset, subscriptions bind to a single process-lifetime token.
    /// </summary>
    /// <param name="provider">Returns the token identifying the current state frame.</param>
    internal void UseFrameTokenProvider(Func<object> provider) => _frameTokenProvider = provider;

    /// <summary>Removes every subscription bound to <paramref name="frameToken"/>. Called on frame teardown.</summary>
    /// <param name="frameToken">The frame token whose subscriptions are cleared.</param>
    internal void ClearSubscriptionsForFrame(object frameToken)
    {
        foreach (var (id, list) in _subscriptions.ToArray())
        {
            list.RemoveAll(subscription => ReferenceEquals(subscription.FrameToken, frameToken));
            if (list.Count == 0)
            {
                _subscriptions.Remove(id);
            }
        }
    }

    /// <summary>Pins the conventional writable (user) source that <see cref="SetUserValue{T}"/> targets.</summary>
    /// <param name="id">The user source id.</param>
    internal void SetUserSource(Identification id) => _userSourceId = id;

    private T Resolve<T>(Identification id)
    {
        ProcessRegisteredSources();

        foreach (var sourceId in _orderedSourceIds)
        {
            if (_sources[sourceId].TryGet(id, out var value) && value is not null)
            {
                return (T)value;
            }
        }

        if (_declarations.TryGetValue(id, out var declaration) && declaration is SettingDefinition<T> definition)
        {
            return definition.Default;
        }

        throw new InvalidOperationException($"Setting '{id}' is not declared.");
    }

    private void Dispatch(Identification id, object? value)
    {
        if (!_subscriptions.TryGetValue(id, out var list))
        {
            return;
        }

        foreach (var subscription in list.ToArray())
        {
            subscription.Callback(value);
        }
    }

    private void RecomputeOrder()
    {
        var builder = new OrderingGraphBuilder<Identification>();
        foreach (var sourceId in _sources.Keys)
        {
            builder.AddNode(sourceId);
        }

        foreach (var (sourceId, source) in _sources)
        {
            foreach (var order in source.OrderBefore)
            {
                builder.AddEdge(sourceId, order.Target, order.Optional);
            }

            foreach (var order in source.OrderAfter)
            {
                builder.AddEdge(order.Target, sourceId, order.Optional);
            }
        }

        var comparer = Comparer<Identification>.Create((left, right) =>
            string.CompareOrdinal(DescribeSource(left), DescribeSource(right)));

        var result = builder.Sort(OrderingTiebreak<Identification>.Lexicographic(comparer));
        _orderedSourceIds = result switch
        {
            Result<IReadOnlyList<Identification>, OrderingError<Identification>>.Ok ok => [.. ok.Value],
            Result<IReadOnlyList<Identification>, OrderingError<Identification>>.Error error => throw OrderingFailure(error.Value),
        };
    }

    private InvalidOperationException OrderingFailure(OrderingError<Identification> error) => error switch
    {
        OrderingError<Identification>.Cycle cycle => new InvalidOperationException(
            $"Setting source ordering cycle among sources: {string.Join(", ", cycle.Participants.Select(DescribeSource))}."),
        OrderingError<Identification>.MissingRequiredDependency missing => new InvalidOperationException(
            $"Setting source ordering edge '{DescribeSource(missing.From)}' -> '{DescribeSource(missing.To)}' references an unregistered source."),
    };

    private string DescribeSource(Identification id) =>
        _sources.TryGetValue(id, out var source) ? source.SourceId : id.ToString();

    private sealed record Subscription(object FrameToken, Action<object?> Callback);

    private sealed class Unsubscriber(List<Subscription> list, Subscription subscription) : IDisposable
    {
        public void Dispose() => list.Remove(subscription);
    }
}
