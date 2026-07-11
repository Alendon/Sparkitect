using JetBrains.Annotations;
using Sparkitect.Events;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Settings;

namespace Sparkitect.Input;

/// <summary>
/// Registry for named, device-decoupled input actions (D-03: mirrors <see cref="SettingRegistry"/>'s
/// stock <c>[Registry]</c> shape, zero new registry machinery). Registering an action makes its own
/// typed id resolvable as <see cref="Identification{T}"/> (falls out of the bare marker, D-04 tier 3)
/// and hand-fans-out into two OTHER registries' id-spaces from the same underlying id: the action's
/// single mandatory default binding (settings-target) and its edge-event analogue (event-target,
/// R-5 — the first-ever exercise of this cross-registry target). The generator emits ONLY the typed
/// aliases; this method body performs the actual runtime fan-out calls (61.3 R-1 trust contract).
/// </summary>
/// <remarks>
/// Declares per-target <see cref="AliasSuffixAttribute{TTargetRegistry}"/> for BOTH cross-registry
/// targets so the settings alias and the event alias — both derived from the same action id — land
/// under distinct, collision-proof names (C-2/61-02; the positive end-to-end proof of Plan 02's
/// per-target-suffix mechanism, the real two-target consumer).
/// </remarks>
[Registry(Identifier = "action")]
[AliasSuffix<SettingRegistry>("Key")]
[AliasSuffix<EventRegistry>("Changed")]
[PublicAPI]
public partial class ActionRegistry(ISettingsManager settingsManager, IEventManagerRegistryFacade eventFacade)
    : IRegistry<InputModule>
{
    /// <inheritdoc/>
    public static string Identifier => "action";

    /// <summary>
    /// Registers an action: its own result type (<typeparamref name="TResult"/>), its single
    /// mandatory default binding (<typeparamref name="TDefaultBindingValue"/>, declared into
    /// <see cref="ISettingsManager"/>), and its edge-event analogue (<typeparamref name="TEventPayload"/>,
    /// registered on the 61.1 event bus). Resolved C-7 (Option A, default-binding-sg): every action
    /// ships exactly one compile-time default binding; extra bindings are added later at runtime via
    /// the D-21 <c>AddBinding</c> verbs and are never SG-aliased.
    /// </summary>
    /// <typeparam name="TResult">The action's own native CLR result type.</typeparam>
    /// <typeparam name="TDefaultBindingValue">The default binding's settings value type.</typeparam>
    /// <typeparam name="TEventPayload">The edge event's payload type.</typeparam>
    /// <param name="id">The action id, shared across all three typed wrappers (own/settings/event).</param>
    /// <param name="description">The action's result type and default-binding value.</param>
    [RegistryMethod]
    public void RegisterAction<
        [TypedIdentification] TResult,
        [TypedIdentification<SettingRegistry>] TDefaultBindingValue,
        [TypedIdentification<EventRegistry>] TEventPayload>(
        Identification id, ActionDescription<TResult, TDefaultBindingValue, TEventPayload> description)
    {
        settingsManager.Declare(
            new Identification<TDefaultBindingValue>(id),
            new SettingDefinition<TDefaultBindingValue>(description.DefaultBinding));
        eventFacade.RegisterEvent<TEventPayload>(id, new EventDefinition<TEventPayload>());
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        settingsManager.Undeclare(id);
        eventFacade.Unregister(id);
    }
}

/// <summary>
/// The registration payload for <see cref="ActionRegistry.RegisterAction{TResult,TDefaultBindingValue,TEventPayload}"/>.
/// Carries all three closed generic type arguments (mirrors <c>DummyRegistry.TypedSettingProvider</c>)
/// so a future registrations-boilerplate value source can infer all three markers through one
/// constructed-generic slot. <typeparamref name="TResult"/> is a phantom parameter — the action's own
/// typed id falls out of the bare marker alone, never a field on this record.
/// </summary>
/// <typeparam name="TResult">The action's own native CLR result type.</typeparam>
/// <typeparam name="TDefaultBindingValue">The default binding's settings value type.</typeparam>
/// <typeparam name="TEventPayload">The edge event's payload type.</typeparam>
/// <param name="DefaultBinding">The default binding's settings value, Declared under the action's id.</param>
[PublicAPI]
public readonly record struct ActionDescription<TResult, TDefaultBindingValue, TEventPayload>(
    TDefaultBindingValue DefaultBinding);
