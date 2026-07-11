using JetBrains.Annotations;
using Sparkitect.Events;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Settings;

namespace Sparkitect.Input;

/// <summary>
/// The single registry for named, device-decoupled input actions. Registering an action makes its
/// own typed id resolvable as <see cref="Identification{T}"/> and informs all three managers from
/// the same underlying id: Settings (the action's single mandatory default binding,
/// settings-target), Events (the action's delivery channel, event-target), and the input
/// implementation (which wires the default binding live). The generator emits ONLY the typed
/// aliases; this method body performs the actual runtime fan-out calls.
/// </summary>
/// <remarks>
/// Declares per-target <see cref="AliasSuffixAttribute{TTargetRegistry}"/> for BOTH cross-registry
/// targets so the settings alias and the event alias — both derived from the same action id — land
/// under distinct, collision-proof names.
/// </remarks>
[Registry(Identifier = "action")]
[AliasSuffix<SettingRegistry>("Key")]
[AliasSuffix<EventRegistry>("Changed")]
[PublicAPI]
public partial class ActionRegistry(
    ISettingsManager settingsManager,
    IEventManagerRegistryFacade eventFacade,
    IInputActionsRegistryFacade inputFacade)
    : IRegistry<InputModule>
{
    /// <inheritdoc/>
    public static string Identifier => "action";

    /// <summary>
    /// Registers an action and informs all three managers: its single mandatory default binding
    /// (<typeparamref name="TDefaultBindingValue"/>, declared into <see cref="ISettingsManager"/>
    /// under the action's own id), its delivery event (payload <typeparamref name="TResult"/>,
    /// registered on the event bus — the channel push bindings ride), and the input
    /// implementation, which wires the default binding live from the declared setting. Every
    /// action ships exactly one compile-time default binding; consumption happens through
    /// <see cref="IInputActions"/> push/pull, never through additional declarations.
    /// </summary>
    /// <typeparam name="TResult">The action's own native CLR result type — also the delivery
    /// event's payload.</typeparam>
    /// <typeparam name="TDefaultBindingValue">The default binding's settings value type: a raw
    /// source value (e.g. a key enum) or a device-neutral shape over one
    /// (<see cref="InputAxis{TKey}"/>, <see cref="InputVector2{TKey}"/>).</typeparam>
    /// <param name="id">The action id, shared across all three typed views (own/settings/event).</param>
    /// <param name="description">The action's result type and default-binding value.</param>
    [RegistryMethod]
    public void RegisterAction<
        [TypedIdentification, TypedIdentification<EventRegistry>] TResult,
        [TypedIdentification<SettingRegistry>] TDefaultBindingValue>(
        Identification id, ActionDescription<TResult, TDefaultBindingValue> description)
    {
        settingsManager.Declare(
            new Identification<TDefaultBindingValue>(id),
            new SettingDefinition<TDefaultBindingValue>(description.DefaultBinding));
        eventFacade.RegisterEvent<TResult>(id, new EventDefinition<TResult>());
        inputFacade.RegisterAction<TResult, TDefaultBindingValue>(id);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        inputFacade.Unregister(id);
        settingsManager.Undeclare(id);
        eventFacade.Unregister(id);
    }
}

/// <summary>
/// The registration payload for <see cref="ActionRegistry.RegisterAction{TResult,TDefaultBindingValue}"/>.
/// Carries both closed generic type arguments so the registrations boilerplate can infer both
/// markers through one constructed-generic slot. <typeparamref name="TResult"/> is a phantom
/// parameter — the action's own typed id and its delivery-event payload fall out of the marker
/// alone, never a field on this record.
/// </summary>
/// <typeparam name="TResult">The action's own native CLR result type — also the delivery event's
/// payload.</typeparam>
/// <typeparam name="TDefaultBindingValue">The default binding's settings value type.</typeparam>
/// <param name="DefaultBinding">The default binding's settings value, declared under the action's id.</param>
[PublicAPI]
public readonly record struct ActionDescription<TResult, TDefaultBindingValue>(
    TDefaultBindingValue DefaultBinding);
