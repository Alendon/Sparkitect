using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Serilog;
using Silk.NET.Input;
using Sparkitect.Events;
using Sparkitect.GameState;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Utils.DU;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.WindowInput;

/// <summary>
/// The first concrete <see cref="IInputActions"/> implementation: owns binding storage
/// (type-bunched by concrete binding type), the source/channel control plane, per-frame
/// sampling + evaluation + first-match composition, and push/pull binding lifetime.
/// <see cref="ActionRegistry"/> informs it of every registered action through
/// <see cref="IInputActionsRegistryFacade"/>; the implementation then wires the action's default
/// binding live from its already-declared setting, translating the device-neutral setting shape
/// through its registered binding adapter for that shape. Push delivery rides the event bus: each
/// composed <c>Value(T)</c> is published under the action's id every frame, and
/// <see cref="Push{T}"/> is a hidden subscription to that event. Core Input never references any
/// of this — <see cref="WindowInputModule"/> reaches the processing and control-plane entry
/// points ONLY through the internal <see cref="IWindowInputActionsStateFacade"/>, resolved by
/// casting the DI-resolved <see cref="IInputActions"/> instance.
/// </summary>
[StateService<IInputActions, WindowInputModule>]
internal sealed class WindowInputActions
    : IInputActions, IInputActionsRegistryFacade, IWindowInputActionsStateFacade, IWindowInputBindings
{
    internal required ISettingsManager SettingsManager { private get; init; }
    internal required IEventManager EventManager { private get; init; }

    private readonly Dictionary<Identification, IActionSlot> _slotsByAction = new();
    private readonly Dictionary<Type, IBindingGroup> _bindingGroupsByType = new();
    private readonly Dictionary<Type, object> _providersByChannel = new();
    private readonly Dictionary<Type, IBindingAdapter> _adaptersBySettingType = new();
    private readonly Dictionary<Identification, ActionRegistration> _actionsById = new();
    private readonly List<IDisposable> _liveBindings = [];

    public WindowInputActions()
    {
        RegisterBindingAdapter<Key, KeyboardKey, bool>(key => new KeyboardKey(key));
        RegisterBindingAdapter<InputAxis<Key>, KeyboardAxis, float>(axis => new KeyboardAxis(axis));
        RegisterBindingAdapter<InputVector2<Key>, KeyboardVector2, Vector2>(vector => new KeyboardVector2(vector));
    }

    /// <inheritdoc/>
    public IPushBinding Push<T>(Identification<T> id, Action<T> callback)
    {
        var subscription = EventManager.Subscribe(id, callback);
        var binding = new PushBindingImpl<T>(this, (Identification)id, subscription);
        _liveBindings.Add(binding);
        return binding;
    }

    /// <inheritdoc/>
    public IPullBinding<T> Pull<T>(Identification<T> id)
    {
        var binding = new PullBindingImpl<T>(this, (Identification)id);
        _liveBindings.Add(binding);
        return binding;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// An action registered before an adapter for its setting shape exists stays pending — a
    /// composition state, never an error — and materializes when the adapter arrives.
    /// </remarks>
    public void RegisterAction<TResult, TDefaultBindingValue>(Identification id)
    {
        var registration = new ActionRegistration(typeof(TResult), typeof(TDefaultBindingValue));
        _actionsById[id] = registration;

        if (_adaptersBySettingType.TryGetValue(registration.SettingType, out var adapter))
        {
            Materialize(id, registration, adapter);
        }
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        if (!_actionsById.Remove(id, out var registration)) return;

        if (registration.LiveBinding is { } entry && _slotsByAction.TryGetValue(id, out var slot))
        {
            slot.Detach(entry.Group, entry.Index);
        }

        _slotsByAction.Remove(id);
    }

    /// <summary>
    /// Registers the adapter that turns a declared default-binding setting value of type
    /// <typeparamref name="TSetting"/> into a live <typeparamref name="TSelf"/> binding instance.
    /// Any pending action whose setting shape matches materializes immediately. This is the same
    /// public seam a gamepad implementation uses to teach the runtime its own shapes — no
    /// privileged engine path.
    /// </summary>
    /// <typeparam name="TSetting">The default-binding settings value type this adapter interprets.</typeparam>
    /// <typeparam name="TSelf">The concrete binding type built from the setting value.</typeparam>
    /// <typeparam name="TResult">The bound action's result type.</typeparam>
    /// <param name="factory">Builds a live binding instance from a settings value — invoked at
    /// materialization and again on every rebind re-resolution.</param>
    public void RegisterBindingAdapter<TSetting, TSelf, TResult>(Func<TSetting, TSelf> factory)
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        var adapter = new BindingAdapter<TSetting, TSelf, TResult>(factory);
        _adaptersBySettingType[typeof(TSetting)] = adapter;

        foreach (var (id, registration) in _actionsById)
        {
            if (registration.LiveBinding is null && registration.SettingType == typeof(TSetting))
            {
                Materialize(id, registration, adapter);
            }
        }
    }

    private void Materialize(Identification id, ActionRegistration registration, IBindingAdapter adapter)
    {
        if (adapter.ResultType != registration.ResultType)
        {
            throw new InvalidOperationException(
                $"Action '{id}' declares result type '{registration.ResultType}', but the binding adapter " +
                $"for setting shape '{registration.SettingType}' produces '{adapter.ResultType}'.");
        }

        registration.LiveBinding = adapter.Attach(this, id);
    }

    /// <summary>
    /// Rebind verb: writes <paramref name="newValue"/> through
    /// <see cref="ISettingsManager.SetUserValue{T}"/> — the substrate is the single source of
    /// truth, this verb never mutates the live binding instance directly. The rebind-dirty
    /// subscription marks the binding for re-resolution at the NEXT <see cref="ProcessFrame"/>;
    /// same-frame rebinds coalesce onto one re-resolution. Composite value-type settings rebind
    /// in ONE atomic write.
    /// </summary>
    /// <typeparam name="TSetting">The binding's settings value type.</typeparam>
    /// <param name="settingId">The action's default-binding setting id.</param>
    /// <param name="newValue">The new settings value.</param>
    public Result<SetError> Rebind<TSetting>(Identification<TSetting> settingId, TSetting newValue) =>
        SettingsManager.SetUserValue(settingId, newValue);

    /// <summary>
    /// Informational conflict reverse-lookup, NEVER a veto (cross-action sharing is legitimate
    /// game policy). Returns every live binding of settings type <typeparamref name="TSetting"/>
    /// whose CURRENT value satisfies <paramref name="referencesSourceValue"/> — the caller
    /// supplies the channel-specific matching logic so the runtime stays channel-agnostic.
    /// </summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    public IReadOnlyList<Identification> FindBindingsReferencing<TSetting>(Func<TSetting, bool> referencesSourceValue)
    {
        List<Identification> matches = [];
        foreach (var (id, registration) in _actionsById)
        {
            if (registration.LiveBinding is not { } entry || entry.SettingType != typeof(TSetting))
            {
                continue;
            }

            if (referencesSourceValue((TSetting)entry.GetCurrentValueBoxed()))
            {
                matches.Add(id);
            }
        }

        return matches;
    }

    /// <summary>The inverse of <see cref="FindBindingsReferencing{TSetting}"/>: scoped to one action.</summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="actionId">The action to scope the reverse-lookup to.</param>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    public IReadOnlyList<Identification> FindBindingsOnActionReferencing<TSetting>(
        Identification actionId, Func<TSetting, bool> referencesSourceValue)
    {
        List<Identification> matches = [];
        if (_actionsById.TryGetValue(actionId, out var registration)
            && registration.LiveBinding is { } entry
            && entry.SettingType == typeof(TSetting)
            && referencesSourceValue((TSetting)entry.GetCurrentValueBoxed()))
        {
            matches.Add(actionId);
        }

        return matches;
    }

    /// <inheritdoc/>
    public SourceBinding RegisterSource<TValue, TRaw>(IInputSourceProvider<TValue, TRaw> provider)
    {
        _providersByChannel[typeof(TValue)] = provider;
        return new SourceBinding(this, typeof(TValue));
    }

    internal void UnregisterSource(Type channelKey) => _providersByChannel.Remove(channelKey);

    /// <inheritdoc/>
    /// <remarks>
    /// (Re)establishes every live binding's rebind-dirty subscription, UNCONDITIONALLY, every time
    /// this is called — from <see cref="WindowInputModule"/>'s frame-enter transition on EVERY
    /// enter, including oscillation replay. Subscriptions are frame-token-scoped and swept
    /// wholesale on frame teardown, so re-subscribing is idempotent and correct. The subscribed
    /// callback ONLY marks the binding dirty; re-resolution happens at
    /// <see cref="ProcessFrame"/>'s dirty-processing step (the quiescent point).
    /// </remarks>
    public void EstablishRebindDirtySubscriptions()
    {
        foreach (var registration in _actionsById.Values)
        {
            if (registration.LiveBinding is { } entry)
            {
                entry.EstablishSubscription(() => entry.Dirty = true);
            }
        }
    }

    /// <inheritdoc/>
    public void ProcessFrame()
    {
        // (1) Re-resolve rebound bindings before sampling.
        ProcessDirtyBindings();

        // (2) Sample + bunched evaluation, one call per distinct concrete binding type.
        var sampling = new SourceSamplingView(_providersByChannel);
        foreach (var group in _bindingGroupsByType.Values)
        {
            group.Sample(sampling);
            group.Evaluate();
        }

        // (3) Combine (first-match-wins) + publish every processed Value(T), every frame.
        foreach (var slot in _slotsByAction.Values)
        {
            slot.Refresh();
        }
    }

    private void ProcessDirtyBindings()
    {
        foreach (var registration in _actionsById.Values)
        {
            if (registration.LiveBinding is not { Dirty: true } entry)
            {
                continue;
            }

            entry.RefreshFromSettings();
            entry.Dirty = false;
        }
    }

    /// <inheritdoc/>
    public void SweepResidualBindings()
    {
        foreach (var binding in _liveBindings.ToArray())
        {
            Log.Warning(
                "WindowInputActions: auto-cleaning a residual {Binding} at state teardown -- it " +
                "should have been disposed by its owner.", binding);
            binding.Dispose();
        }

        _liveBindings.Clear();
    }

    private ActionSlot<TResult> GetOrAddSlot<TResult>(Identification bareId)
    {
        if (_slotsByAction.TryGetValue(bareId, out var existing))
        {
            return (ActionSlot<TResult>)existing;
        }

        var slot = new ActionSlot<TResult>(value => EventManager.Publish(new Identification<TResult>(bareId), value));
        _slotsByAction[bareId] = slot;
        return slot;
    }

    private BindingGroup<TSelf, TResult> GetOrAddGroup<TSelf, TResult>()
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        var key = typeof(TSelf);
        if (_bindingGroupsByType.TryGetValue(key, out var existing))
        {
            return (BindingGroup<TSelf, TResult>)existing;
        }

        var created = new BindingGroup<TSelf, TResult>();
        _bindingGroupsByType[key] = created;
        return created;
    }

    private ActionResult<T> GetCurrent<T>(Identification bareId) =>
        _slotsByAction.TryGetValue(bareId, out var slot) ? ((ActionSlot<T>)slot).Current : ActionResult<T>.NoValue;

    private void UntrackBinding(IDisposable binding) => _liveBindings.Remove(binding);

    /// <summary>A registered action: its declared types plus its live default binding, if materialized.</summary>
    private sealed class ActionRegistration(Type resultType, Type settingType)
    {
        internal Type ResultType { get; } = resultType;
        internal Type SettingType { get; } = settingType;
        internal BindingInstanceEntry? LiveBinding { get; set; }
    }

    /// <summary>
    /// Turns a declared default-binding setting value into a live binding instance attached to
    /// the action's evaluation set. Generic over the full (setting, binding, result) triple so
    /// the registration path can stay type-erased.
    /// </summary>
    private interface IBindingAdapter
    {
        Type ResultType { get; }
        BindingInstanceEntry Attach(WindowInputActions owner, Identification actionId);
    }

    private sealed class BindingAdapter<TSetting, TSelf, TResult>(Func<TSetting, TSelf> factory) : IBindingAdapter
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        public Type ResultType => typeof(TResult);

        public BindingInstanceEntry Attach(WindowInputActions owner, Identification actionId)
        {
            var settingId = new Identification<TSetting>(actionId);
            var instance = factory(owner.SettingsManager.GetValue(settingId));
            var slot = owner.GetOrAddSlot<TResult>(actionId);
            var group = owner.GetOrAddGroup<TSelf, TResult>();
            var index = group.Add(instance);
            slot.AddBinding(group, index);

            return new BindingInstanceEntry(
                settingType: typeof(TSetting),
                group: group,
                index: index,
                refreshFromSettings: () => group.Replace(index, factory(owner.SettingsManager.GetValue(settingId))),
                establishSubscription: markDirty => owner.SettingsManager.Subscribe(settingId, _ => markDirty()),
                getCurrentValueBoxed: () => owner.SettingsManager.GetValue(settingId)!);
        }
    }

    private interface IActionSlot
    {
        void Refresh();

        /// <summary>
        /// Detaches the (group, index) pair from this action's evaluation set. <paramref name="group"/>
        /// is boxed to <c>object</c> by the type-erased caller — reference identity still matches
        /// regardless of static type, so no cast is needed.
        /// </summary>
        void Detach(object group, int index);
    }

    private interface IResultLookup<TResult>
    {
        ActionResult<TResult> Result(int index);
    }

    private interface IBindingGroup
    {
        void Sample(IInputSourceSampling sampling);
        void Evaluate();
    }

    /// <summary>
    /// One action's ordered binding set and current pull-readable result. Combination walks
    /// bindings in stored (add) order, first non-<c>NoValue</c> wins; every processed
    /// <c>Value(T)</c> is published to the action's event — every frame, including a repeated
    /// identical value — never gated on a frame-to-frame transition check.
    /// </summary>
    private sealed class ActionSlot<TResult>(Action<TResult> publish) : IActionSlot
    {
        private readonly List<(IResultLookup<TResult> Group, int Index)> _bindings = [];

        internal ActionResult<TResult> Current { get; private set; } = ActionResult<TResult>.NoValue;

        internal void AddBinding(IResultLookup<TResult> group, int index) => _bindings.Add((group, index));

        public void Detach(object group, int index) =>
            _bindings.RemoveAll(binding => ReferenceEquals(binding.Group, group) && binding.Index == index);

        public void Refresh()
        {
            var combined = ActionResult<TResult>.NoValue;
            foreach (var (group, index) in _bindings)
            {
                var candidate = group.Result(index);
                if (!candidate.HasValue) continue;
                combined = candidate;
                break;
            }

            Current = combined;

            if (combined.HasValue)
            {
                publish(combined.Value());
            }
        }
    }

    /// <summary>
    /// The type-bunched sample+evaluate unit for one concrete binding type: every instance of
    /// <typeparamref name="TSelf"/> across every action lives in ONE contiguous list, sampled and
    /// evaluated with one call per frame — O(distinct binding types), not O(bindings).
    /// </summary>
    private sealed class BindingGroup<TSelf, TResult> : IBindingGroup, IResultLookup<TResult>
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        private readonly List<TSelf> _instances = [];
        private ActionResult<TResult>[] _results = [];

        internal int Add(TSelf instance)
        {
            var index = _instances.Count;
            _instances.Add(instance);
            return index;
        }

        /// <summary>
        /// Rebind re-resolution: overwrites the live instance at <paramref name="index"/>. Called
        /// only from a <see cref="BindingInstanceEntry.RefreshFromSettings"/> closure, never
        /// per-frame.
        /// </summary>
        internal void Replace(int index, TSelf instance)
        {
            if (index >= 0 && index < _instances.Count)
            {
                _instances[index] = instance;
            }
        }

        public void Sample(IInputSourceSampling sampling) =>
            TSelf.Sample(CollectionsMarshal.AsSpan(_instances), sampling);

        public void Evaluate()
        {
            if (_results.Length != _instances.Count)
            {
                _results = new ActionResult<TResult>[_instances.Count];
            }

            TSelf.Evaluate(CollectionsMarshal.AsSpan(_instances), _results);
        }

        public ActionResult<TResult> Result(int index) =>
            index >= 0 && index < _results.Length ? _results[index] : ActionResult<TResult>.NoValue;
    }

    /// <summary>
    /// Bookkeeping for one live default binding. Type-erased over the setting/binding types via
    /// closures captured at attach time, so rebind re-resolution, the conflict queries, and
    /// <see cref="EstablishRebindDirtySubscriptions"/> can all walk one non-generic collection.
    /// </summary>
    private sealed class BindingInstanceEntry(
        Type settingType,
        object group,
        int index,
        Action refreshFromSettings,
        Func<Action, IDisposable> establishSubscription,
        Func<object> getCurrentValueBoxed)
    {
        internal Type SettingType { get; } = settingType;
        internal object Group { get; } = group;
        internal int Index { get; } = index;
        internal Action RefreshFromSettings { get; } = refreshFromSettings;
        internal Func<Action, IDisposable> EstablishSubscription { get; } = establishSubscription;
        internal Func<object> GetCurrentValueBoxed { get; } = getCurrentValueBoxed;

        /// <summary>Set by the rebind-dirty subscription callback; cleared by dirty-processing.</summary>
        internal bool Dirty { get; set; }
    }

    /// <summary>
    /// Routes a binding type's <see cref="IBindingType{TSelf,TResult}.Sample"/> lookups to the
    /// provider registered for that channel; a providerless channel leaves the results span
    /// untouched — never faults.
    /// </summary>
    private sealed class SourceSamplingView(Dictionary<Type, object> providersByChannel) : IInputSourceSampling
    {
        public void Sample<TValue, TRaw>(ReadOnlySpan<TValue> values, Span<TRaw> results)
        {
            if (!providersByChannel.TryGetValue(typeof(TValue), out var providerObj)) return;
            ((IInputSourceProvider<TValue, TRaw>)providerObj).Sample(values, results);
        }
    }

    private sealed class PushBindingImpl<T>(WindowInputActions owner, Identification bareId, EventBinding subscription)
        : IPushBinding
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            subscription.Dispose();
            owner.UntrackBinding(this);
        }

        public override string ToString() => $"push binding for action '{bareId}'";
    }

    private sealed class PullBindingImpl<T>(WindowInputActions owner, Identification bareId) : IPullBinding<T>
    {
        private bool _disposed;

        public ActionResult<T> Read()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(PullBindingImpl<T>), $"Pull binding for action '{bareId}' has been disposed.");
            }

            return owner.GetCurrent<T>(bareId);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner.UntrackBinding(this);
        }

        public override string ToString() => $"pull binding for action '{bareId}'";
    }
}

/// <summary>
/// The WindowInput implementation's public binding surface: teaches the runtime new setting-shape
/// adapters, rebinds an action's default binding through Settings, and runs the informational
/// reverse-conflict lookup. Reached by casting the DI-resolved <see cref="IInputActions"/>
/// instance to this PUBLIC interface — unlike <see cref="IWindowInputActionsStateFacade"/>
/// (engine-only), this surface is extension-facing by design.
/// </summary>
[PublicAPI]
public interface IWindowInputBindings
{
    /// <inheritdoc cref="WindowInputActions.RegisterBindingAdapter{TSetting,TSelf,TResult}"/>
    void RegisterBindingAdapter<TSetting, TSelf, TResult>(Func<TSetting, TSelf> factory)
        where TSelf : struct, IBindingType<TSelf, TResult>;

    /// <inheritdoc cref="WindowInputActions.Rebind{TSetting}"/>
    Result<SetError> Rebind<TSetting>(Identification<TSetting> settingId, TSetting newValue);

    /// <inheritdoc cref="WindowInputActions.FindBindingsReferencing{TSetting}"/>
    IReadOnlyList<Identification> FindBindingsReferencing<TSetting>(Func<TSetting, bool> referencesSourceValue);

    /// <inheritdoc cref="WindowInputActions.FindBindingsOnActionReferencing{TSetting}"/>
    IReadOnlyList<Identification> FindBindingsOnActionReferencing<TSetting>(
        Identification actionId, Func<TSetting, bool> referencesSourceValue);
}

/// <summary>
/// Engine-internal entry points for <see cref="WindowInputActions"/> (never mod-facing): the
/// per-frame processing pass, the residual-binding teardown sweep, and the source/channel
/// control plane. Reached by casting the DI-resolved <see cref="IInputActions"/> instance — both
/// interfaces are implemented by the one registered <see cref="WindowInputActions"/> instance, so
/// no separate DI registration is needed and core Input stays unaware this facade exists.
/// </summary>
internal interface IWindowInputActionsStateFacade
{
    /// <summary>
    /// Runs one per-frame processing pass: samples every registered binding group's raw channel
    /// slots, evaluates them, first-match-composes each action's result into its pull-readable
    /// slot, then publishes every processed <c>Value(T)</c> to the action's event — every frame,
    /// dropping only <c>NoValue</c>.
    /// </summary>
    void ProcessFrame();

    /// <summary>
    /// Residual sweep: auto-cleans any still-live push/pull binding with a provenance-rich
    /// warning, without interrupting teardown. Idempotent — a binding removes itself from
    /// tracking on dispose, so a repeat sweep is a harmless no-op.
    /// </summary>
    void SweepResidualBindings();

    /// <summary>
    /// (Re)establishes every live binding's rebind-dirty subscription, unconditionally. Called
    /// from <see cref="WindowInputModule"/>'s frame-enter transition on every enter, including
    /// oscillation replay.
    /// </summary>
    void EstablishRebindDirtySubscriptions();

    /// <summary>
    /// Registers a bulk-fill source provider for one channel vocabulary. A channel with no
    /// registered provider is a composition state, never an error: bindings referencing it simply
    /// have nothing refresh their raw slots this frame.
    /// </summary>
    /// <typeparam name="TValue">The channel's value vocabulary (e.g. a key enum).</typeparam>
    /// <typeparam name="TRaw">The raw sampled result shape for one channel value.</typeparam>
    /// <param name="provider">The bulk-fill provider for this channel.</param>
    /// <returns>A disposable binding whose disposal unregisters <paramref name="provider"/>.</returns>
    SourceBinding RegisterSource<TValue, TRaw>(IInputSourceProvider<TValue, TRaw> provider);
}

/// <summary>
/// Disposable binding returned by
/// <see cref="IWindowInputActionsStateFacade.RegisterSource{TValue,TRaw}"/> (mirrors the
/// <see cref="Sparkitect.Events.EventBinding"/> idiom). Disposing unregisters the provider.
/// </summary>
[PublicAPI]
public readonly struct SourceBinding : IDisposable
{
    private readonly WindowInputActions? _actions;
    private readonly Type _channelKey;

    internal SourceBinding(WindowInputActions actions, Type channelKey)
    {
        _actions = actions;
        _channelKey = channelKey;
    }

    /// <inheritdoc/>
    public void Dispose() => _actions?.UnregisterSource(_channelKey);
}
