using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Sparkitect.Events;
using Sparkitect.GameState;
using Sparkitect.Input.Bindings;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Utils.DU;

namespace Sparkitect.Input;

/// <summary>
/// The input engine's result-slot store (D-04 tier 2, processed/stored variation). Eagerly
/// constructed by DI before transition functions run (per the state-service construction
/// constraint) — this ctor does no build work; all mutation lives in verbs
/// (<see cref="Handle{TResult}"/>, <see cref="RegisterSource{TValue,TRaw}"/>,
/// <see cref="RegisterBinding{TSelf,TResult}"/>, <see cref="AddBinding{TSelf,TSetting,TResult}"/>,
/// <see cref="RemoveBinding"/>, <see cref="Rebind{TSetting}"/>) and the per-frame
/// <see cref="BuildSnapshot"/> pass.
/// </summary>
[StateService<IInputManager, InputModule>]
internal sealed class InputManager : IInputManager, IInputManagerStateFacade
{
    internal required IEventManager EventManager { private get; init; }

    internal required ISettingsManager SettingsManager { private get; init; }

    // Bare Identification key: the same action id is handed out through many typed wrappers
    // (Identification<TResult>) that all wrap one underlying value (D-04 tier 3).
    private readonly Dictionary<Identification, ActionHandle> _handlesByAction = new();
    private readonly List<IActionSlot> _slots = [];

    // Type-bunched binding storage (D-18): one group per concrete binding type, shared across every
    // action that uses it -- Evaluate() runs once per group per frame, not once per instance.
    private readonly Dictionary<Type, IBindingGroup> _bindingGroupsByType = new();

    // Control plane (D-13): providers keyed by their channel value vocabulary. A channel with no
    // registered provider is a composition state (D-20) -- nothing here ever faults on a miss.
    private readonly Dictionary<Type, object> _providersByChannel = new();

    // D-21 rebind bookkeeping: every AddBinding-created binding instance, keyed by its own (bare)
    // settings id -- the conflict query (D-23), RemoveBinding, and the rebind-dirty subscription
    // (D-22/Pitfall 1) all walk this. Every entry here is a USER-added instance (RESEARCH Pattern
    // 6): an action's single default binding is authored inline at ActionRegistry.RegisterAction,
    // never through AddBinding.
    private readonly Dictionary<Identification, BindingInstanceEntry> _bindingEntriesBySettingId = new();

    private uint _nextIndex;
    private uint _nextGeneration;

    /// <inheritdoc/>
    public ActionHandle Handle<TResult>(Identification<TResult> id)
    {
        var bareId = (Identification)id;
        if (_handlesByAction.TryGetValue(bareId, out var existing))
        {
            return existing;
        }

        var handle = new ActionHandle(_nextIndex++, _nextGeneration++);
        _handlesByAction[bareId] = handle;
        _slots.Add(new ActionSlot<TResult>(bareId));
        return handle;
    }

    /// <inheritdoc/>
    public ActionResult<TResult> Read<TResult>(ActionHandle handle)
    {
        if (handle.Index >= _slots.Count)
        {
            return ActionResult<TResult>.NoValue;
        }

        return ((ActionSlot<TResult>)_slots[(int)handle.Index]).Current;
    }

    /// <inheritdoc/>
    public SourceBinding RegisterSource<TValue, TRaw>(IInputSourceProvider<TValue, TRaw> provider)
    {
        var channelKey = typeof(TValue);
        _providersByChannel[channelKey] = provider;
        return new SourceBinding(this, channelKey);
    }

    internal void UnregisterSource(Type channelKey) => _providersByChannel.Remove(channelKey);

    /// <summary>
    /// The raw structural primitive the settings-backed rebind verbs (D-21, Plan 08) build on: attaches
    /// a binding instance to an action's type-bunched evaluation set, in stored order (first-match-wins,
    /// D-19, walks this order). Allocates the action's result slot idempotently
    /// (mirrors <see cref="Handle{TResult}"/>) if not already present.
    /// </summary>
    /// <typeparam name="TSelf">The concrete binding type.</typeparam>
    /// <typeparam name="TResult">The declaring action's result type.</typeparam>
    /// <param name="actionId">The action this binding instance contributes to.</param>
    /// <param name="binding">The binding instance (already carrying its settings-backing + any
    /// currently-sampled raw channel slots).</param>
    internal void RegisterBinding<TSelf, TResult>(Identification<TResult> actionId, TSelf binding)
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        var handle = Handle(actionId);
        var group = GetOrAddGroup<TSelf, TResult>();
        var index = group.Add(binding);
        ((ActionSlot<TResult>)_slots[(int)handle.Index]).AddBinding(group, index);
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

    /// <summary>
    /// D-21 rebind verb: declares a NEW per-binding-instance setting (runtime
    /// <see cref="ISettingsManager.Declare{T}"/>) and attaches a live binding instance built from
    /// its current settings value via <paramref name="factory"/> — raw settings stay the substrate,
    /// this verb is the contract (mirrors <c>WindowManager.DestroyWindow</c>). Every binding this
    /// verb creates is a USER-added instance (RESEARCH Pattern 6): an action's single default
    /// binding is authored inline at <c>ActionRegistry.RegisterAction</c>, never through this verb.
    /// Also records the re-resolution recipe (D-22) and the subscribe recipe (Pitfall 1) that
    /// <see cref="EstablishRebindDirtySubscriptions"/> and <see cref="ProcessDirtyBindings"/> consume.
    /// </summary>
    /// <typeparam name="TSelf">The concrete binding type.</typeparam>
    /// <typeparam name="TSetting">The binding-backing setting's value type — may be a composite
    /// value type (D-17); only the binding type itself knows this shape (D-16), Input core stays
    /// channel-agnostic.</typeparam>
    /// <typeparam name="TResult">The declaring action's result type.</typeparam>
    /// <param name="actionId">The action this binding instance contributes to.</param>
    /// <param name="settingId">The fresh id this binding instance's setting is declared under.</param>
    /// <param name="initialValue">The setting's initial value.</param>
    /// <param name="factory">Builds a live binding instance from a settings value — invoked once now
    /// and again on every dirty re-resolution (D-22 snapshot-start quiescent point).</param>
    public BindingInstanceHandle AddBinding<TSelf, TSetting, TResult>(
        Identification<TResult> actionId,
        Identification<TSetting> settingId,
        TSetting initialValue,
        Func<TSetting, TSelf> factory)
        where TSelf : struct, IBindingType<TSelf, TResult>
    {
        SettingsManager.Declare(settingId, new SettingDefinition<TSetting>(initialValue));
        var instance = factory(SettingsManager.GetValue(settingId));

        var handle = Handle(actionId);
        var group = GetOrAddGroup<TSelf, TResult>();
        var index = group.Add(instance);
        ((ActionSlot<TResult>)_slots[(int)handle.Index]).AddBinding(group, index);

        var bareSettingId = (Identification)settingId;
        var entry = new BindingInstanceEntry(
            actionId: (Identification)actionId,
            settingType: typeof(TSetting),
            group: group,
            index: index,
            refreshFromSettings: () => group.Replace(index, factory(SettingsManager.GetValue(settingId))),
            establishSubscription: markDirty => SettingsManager.Subscribe(settingId, _ => markDirty()),
            undeclare: () => SettingsManager.Undeclare(settingId),
            getCurrentValueBoxed: () => SettingsManager.GetValue(settingId)!);

        _bindingEntriesBySettingId[bareSettingId] = entry;
        return new BindingInstanceHandle(bareSettingId);
    }

    /// <summary>
    /// D-21 rebind verb: undeclares the binding instance's setting and detaches it from its
    /// action's evaluation set. Every <paramref name="handle"/> <see cref="AddBinding{TSelf,TSetting,TResult}"/>
    /// returns is a user-added instance (RESEARCH Pattern 6) — removing it is a genuine
    /// <see cref="ISettingsManager.Undeclare"/>. An action's single DEFAULT binding (authored inline
    /// at <c>ActionRegistry.RegisterAction</c>) is never reachable through this verb: no live
    /// default-binding instance is wired into the evaluation set yet (flagged since Plans 05/07).
    /// Doctrine decision for when that wiring lands: reset-to-defaults should use a per-instance
    /// "enabled" overlay rather than <see cref="ISettingsManager.Undeclare"/>, so the default layer
    /// survives — undeclaring it outright would break reset-to-defaults.
    /// </summary>
    /// <param name="handle">A handle previously returned by <see cref="AddBinding{TSelf,TSetting,TResult}"/>.</param>
    public void RemoveBinding(BindingInstanceHandle handle)
    {
        if (!_bindingEntriesBySettingId.Remove(handle.SettingId, out var entry))
        {
            return;
        }

        entry.Undeclare();

        if (_handlesByAction.TryGetValue(entry.ActionId, out var actionHandle) && actionHandle.Index < _slots.Count)
        {
            _slots[(int)actionHandle.Index].Detach(entry.Group, entry.Index);
        }
    }

    /// <summary>
    /// D-21 rebind verb: writes <paramref name="newValue"/> through
    /// <see cref="ISettingsManager.SetUserValue{T}"/> — the substrate is the single source of
    /// truth, this verb never mutates the live binding instance directly. When the binding's
    /// rebind-dirty subscription is currently established (D-22/Pitfall 1), the synchronous
    /// effective-change dispatch marks it dirty for the NEXT <see cref="BuildSnapshot"/>'s
    /// dirty-processing pass; same-frame rebinds coalesce onto one re-resolution. Composite
    /// value-type settings (D-17, R-6) rebind in ONE atomic write — never assembled from partial
    /// field writes.
    /// </summary>
    /// <typeparam name="TSetting">The binding's settings value type.</typeparam>
    /// <param name="handle">A handle previously returned by <see cref="AddBinding{TSelf,TSetting,TResult}"/>.</param>
    /// <param name="newValue">The new settings value.</param>
    public Result<SetError> Rebind<TSetting>(BindingInstanceHandle handle, TSetting newValue) =>
        SettingsManager.SetUserValue(new Identification<TSetting>(handle.SettingId), newValue);

    /// <summary>
    /// D-23 conflict reverse-lookup: informational only, NEVER a veto (first-match ordering already
    /// resolves in-action collisions; cross-action sharing is legitimate game policy). Returns every
    /// live <see cref="AddBinding{TSelf,TSetting,TResult}"/>-created binding of settings type
    /// <typeparamref name="TSetting"/> whose CURRENT value satisfies
    /// <paramref name="referencesSourceValue"/> — the caller supplies the channel-specific matching
    /// logic (e.g. "does this setting reference Key.W") so Input core stays channel-agnostic (D-16).
    /// </summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    public IReadOnlyList<Identification> FindBindingsReferencing<TSetting>(Func<TSetting, bool> referencesSourceValue)
    {
        List<Identification> matches = [];
        foreach (var (settingId, entry) in _bindingEntriesBySettingId)
        {
            if (entry.SettingType != typeof(TSetting))
            {
                continue;
            }

            if (referencesSourceValue((TSetting)entry.GetCurrentValueBoxed()))
            {
                matches.Add(settingId);
            }
        }

        return matches;
    }

    /// <summary>The inverse of <see cref="FindBindingsReferencing{TSetting}"/> (D-23): scoped to one action.</summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="actionId">The action to scope the reverse-lookup to.</param>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    public IReadOnlyList<Identification> FindBindingsOnActionReferencing<TSetting>(
        Identification actionId, Func<TSetting, bool> referencesSourceValue)
    {
        List<Identification> matches = [];
        foreach (var (settingId, entry) in _bindingEntriesBySettingId)
        {
            if (entry.SettingType != typeof(TSetting) || entry.ActionId != actionId)
            {
                continue;
            }

            if (referencesSourceValue((TSetting)entry.GetCurrentValueBoxed()))
            {
                matches.Add(settingId);
            }
        }

        return matches;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// D-22/D-26/Pitfall-1: (re)establishes every live binding's rebind-dirty settings subscription,
    /// UNCONDITIONALLY, every time this is called — called from <see cref="InputModule"/>'s
    /// <c>[OnFrameEnterScheduling]</c> transition function on EVERY frame-enter, including
    /// oscillation replay (<c>GameStateManager.TransitionToParent</c> re-runs the parent's enter
    /// methods on every pop/push, <c>GameStateManager.cs:768-773</c>). Subscriptions are
    /// frame-token-scoped and swept wholesale on frame teardown, so the PREVIOUS subscription is
    /// already gone by the time this runs again — simply re-subscribing is idempotent and correct.
    /// The subscribed callback ONLY marks the binding dirty; re-resolution happens at
    /// <see cref="BuildSnapshot"/>'s dirty-processing step (D-22 snapshot-start quiescent point).
    /// </remarks>
    public void EstablishRebindDirtySubscriptions()
    {
        foreach (var entry in _bindingEntriesBySettingId.Values)
        {
            entry.EstablishSubscription(() => entry.Dirty = true);
        }
    }

    /// <inheritdoc/>
    public void BuildSnapshot()
    {
        // (1) D-22 dirty-processing entry point.
        ProcessDirtyBindings();

        // (2) Bunched evaluation (D-18): one Evaluate() call per distinct concrete binding type,
        // regardless of how many instances or actions use it.
        foreach (var group in _bindingGroupsByType.Values)
        {
            group.Evaluate();
        }

        // (3) Combine (first-match-wins, D-19) + edge-detect + publish (D-25).
        foreach (var slot in _slots)
        {
            slot.Refresh(EventManager);
        }
    }

    /// <summary>
    /// D-22 dirty-processing entry point: <see cref="EstablishRebindDirtySubscriptions"/>'s subscribed
    /// callbacks mark bindings dirty here for re-resolution before sampling. Same-frame rebinds
    /// coalesce naturally -- Settings itself already resolves multiple same-frame writes down to one
    /// current value, so one re-resolution call per dirty binding reads that final value.
    /// </summary>
    private void ProcessDirtyBindings()
    {
        foreach (var entry in _bindingEntriesBySettingId.Values)
        {
            if (!entry.Dirty)
            {
                continue;
            }

            entry.RefreshFromSettings();
            entry.Dirty = false;
        }
    }

    private interface IActionSlot
    {
        void Refresh(IEventManager eventManager);
        void Detach(object group, int index);
    }

    private interface IResultLookup<TResult>
    {
        ActionResult<TResult> Result(int index);
    }

    private interface IBindingGroup
    {
        void Evaluate();
    }

    /// <summary>
    /// One action's ordered binding set + its current/previous result (D-18/D-19/D-25). Combination
    /// walks bindings in stored (add) order, first non-<c>NoValue</c> wins; the previous-vs-current
    /// transition is what drives the edge publish -- the same values a poll of <see cref="Current"/>
    /// would observe (INPT-04 no-double-handling).
    /// </summary>
    private sealed class ActionSlot<TResult>(Identification bareId) : IActionSlot
    {
        private readonly List<(IResultLookup<TResult> Group, int Index)> _bindings = [];

        internal ActionResult<TResult> Current { get; private set; } = ActionResult<TResult>.NoValue;
        private ActionResult<TResult> Previous { get; set; } = ActionResult<TResult>.NoValue;

        internal void AddBinding(IResultLookup<TResult> group, int index) => _bindings.Add((group, index));

        /// <summary>
        /// D-21 <c>RemoveBinding</c> support: detaches the (group, index) pair from this action's
        /// evaluation set. <paramref name="group"/> is boxed to <c>object</c> by the type-erased
        /// caller (<see cref="BindingInstanceEntry"/>) -- reference identity still matches regardless
        /// of static type, so no cast is needed.
        /// </summary>
        public void Detach(object group, int index) =>
            _bindings.RemoveAll(binding => ReferenceEquals(binding.Group, group) && binding.Index == index);

        public void Refresh(IEventManager eventManager)
        {
            Previous = Current;

            var combined = ActionResult<TResult>.NoValue;
            foreach (var (group, index) in _bindings)
            {
                var candidate = group.Result(index);
                if (!candidate.HasValue)
                {
                    continue;
                }

                combined = candidate;
                break;
            }

            Current = combined;

            // Edge-detect on the result slot: never a fabricated default, never published from NoValue.
            if (!Previous.HasValue && Current.HasValue)
            {
                eventManager.Publish(new Identification<TResult>(bareId), Current.Value());
            }
        }
    }

    /// <summary>
    /// The type-bunched evaluation unit for one concrete binding type (D-18): every instance of
    /// <typeparamref name="TSelf"/> across every action lives in ONE contiguous list, evaluated with a
    /// single <see cref="IBindingType{TSelf,TResult}.Evaluate"/> call per frame -- O(distinct binding
    /// types), not O(bindings).
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
        /// D-22 dirty re-resolution: overwrites the live instance at <paramref name="index"/>. Called
        /// only from a <see cref="BindingInstanceEntry.RefreshFromSettings"/> closure (D-21's
        /// <see cref="AddBinding{TSelf,TSetting,TResult}"/>), never per-frame.
        /// </summary>
        internal void Replace(int index, TSelf instance)
        {
            if (index >= 0 && index < _instances.Count)
            {
                _instances[index] = instance;
            }
        }

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
    /// D-21 bookkeeping for one <see cref="AddBinding{TSelf,TSetting,TResult}"/>-created binding
    /// instance. Type-erased over <c>TSelf</c>/<c>TSetting</c> via closures captured at AddBinding
    /// time, so <see cref="RemoveBinding"/>, <see cref="Rebind{TSetting}"/>, the conflict query, and
    /// <see cref="EstablishRebindDirtySubscriptions"/> can all walk one non-generic collection.
    /// </summary>
    private sealed class BindingInstanceEntry(
        Identification actionId,
        Type settingType,
        object group,
        int index,
        Action refreshFromSettings,
        Func<Action, IDisposable> establishSubscription,
        Action undeclare,
        Func<object> getCurrentValueBoxed)
    {
        internal Identification ActionId { get; } = actionId;
        internal Type SettingType { get; } = settingType;
        internal object Group { get; } = group;
        internal int Index { get; } = index;
        internal Action RefreshFromSettings { get; } = refreshFromSettings;
        internal Func<Action, IDisposable> EstablishSubscription { get; } = establishSubscription;
        internal Action Undeclare { get; } = undeclare;
        internal Func<object> GetCurrentValueBoxed { get; } = getCurrentValueBoxed;

        /// <summary>Set by the rebind-dirty subscription callback; cleared by <c>ProcessDirtyBindings</c>.</summary>
        internal bool Dirty { get; set; }
    }
}

/// <summary>
/// Disposable binding returned by <see cref="IInputManager.RegisterSource{TValue,TRaw}"/> (mirrors the
/// <see cref="Sparkitect.Events.EventBinding"/> idiom). Disposing unregisters the provider.
/// </summary>
[PublicAPI]
public readonly struct SourceBinding : IDisposable
{
    private readonly InputManager? _manager;
    private readonly Type _channelKey;

    internal SourceBinding(InputManager manager, Type channelKey)
    {
        _manager = manager;
        _channelKey = channelKey;
    }

    /// <inheritdoc/>
    public void Dispose() => _manager?.UnregisterSource(_channelKey);
}

/// <summary>
/// Opaque handle to a live binding instance created by
/// <see cref="IInputManager.AddBinding{TSelf,TSetting,TResult}"/> (D-21) — the input to
/// <see cref="IInputManager.Rebind{TSetting}"/> and <see cref="IInputManager.RemoveBinding"/>.
/// Identity is the binding's own settings id.
/// </summary>
/// <param name="SettingId">The binding instance's own (bare) settings id.</param>
[PublicAPI]
public readonly record struct BindingInstanceHandle(Identification SettingId);
