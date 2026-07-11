using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Input.Bindings;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Utils.DU;

namespace Sparkitect.Input;

/// <summary>
/// Consumer-facing poll surface for input actions (D-04 tier 3), plus the trigger-agnostic source
/// control plane (D-13) and the D-21 rebind verbs. Consumers resolve a typed <see cref="ActionHandle"/>
/// once via <see cref="Handle{TResult}"/> and thereafter read pre-evaluated result slots through
/// <see cref="Read{TResult}"/> — a pure typed read, never a re-run of binding evaluation (mirrors
/// <see cref="Sparkitect.ECS.StorageHandle"/> consumer usage). <see cref="RegisterSource{TValue,TRaw}"/>
/// is open to any module or mod naming its own channel vocabulary (a gamepad mod registering on
/// hotplug is exactly as valid as the bridge registering on <c>WindowCreated</c>) — Input never learns
/// WHY a source appeared. <see cref="AddBinding{TSelf,TSetting,TResult}"/>/<see cref="RemoveBinding"/>/
/// <see cref="Rebind{TSetting}"/> choreograph <see cref="ISettingsManager"/>'s Declare/Undeclare/
/// SetUserValue (D-21) — raw settings stay the substrate, these verbs are the contract.
/// </summary>
[StateFacade<IInputManagerStateFacade>]
[PublicAPI]
public interface IInputManager
{
    /// <summary>
    /// Resolves the result-slot handle for <paramref name="id"/>, allocating a slot on first request
    /// (idempotent — repeat calls for the same id return the same handle; safe under oscillation replay,
    /// D-26).
    /// </summary>
    /// <typeparam name="TResult">The action's native CLR result type.</typeparam>
    /// <param name="id">The action's own typed identification.</param>
    ActionHandle Handle<TResult>(Identification<TResult> id);

    /// <summary>
    /// Reads the pre-evaluated result for <paramref name="handle"/> — the value <c>build_input_snapshot</c>
    /// computed for the current frame. Never re-runs binding evaluation.
    /// </summary>
    /// <typeparam name="TResult">The action's native CLR result type.</typeparam>
    /// <param name="handle">A handle previously obtained from <see cref="Handle{TResult}"/>.</param>
    ActionResult<TResult> Read<TResult>(ActionHandle handle);

    /// <summary>
    /// Registers a bulk-fill source provider for one channel vocabulary (D-13/D-14): control-plane
    /// mutation, trigger-agnostic (the caller's reason for registering — window creation, gamepad
    /// hotplug, a test fixture — is invisible to Input). A channel with no registered provider is a
    /// composition state, never an error (D-20): bindings referencing it simply have nothing refresh
    /// their raw slots this frame, never a fault.
    /// </summary>
    /// <typeparam name="TValue">The channel's value vocabulary (e.g. a key enum).</typeparam>
    /// <typeparam name="TRaw">The raw sampled result shape for one channel value.</typeparam>
    /// <param name="provider">The bulk-fill provider for this channel.</param>
    /// <returns>
    /// A disposable binding (mirrors <see cref="Sparkitect.Events.EventBinding"/>) whose disposal
    /// unregisters <paramref name="provider"/>.
    /// </returns>
    SourceBinding RegisterSource<TValue, TRaw>(IInputSourceProvider<TValue, TRaw> provider);

    /// <summary>
    /// D-21 rebind verb: declares a NEW per-binding-instance setting (runtime
    /// <see cref="ISettingsManager.Declare{T}"/>) and attaches a live binding instance built from its
    /// current settings value via <paramref name="factory"/>. Every binding this verb creates is a
    /// USER-added instance (RESEARCH Pattern 6): an action's single default binding is authored
    /// inline at <c>ActionRegistry.RegisterAction</c>, never through this verb.
    /// </summary>
    /// <typeparam name="TSelf">The concrete binding type.</typeparam>
    /// <typeparam name="TSetting">The binding-backing setting's value type — may be a composite
    /// value type (D-17); only the binding type itself knows this shape (D-16).</typeparam>
    /// <typeparam name="TResult">The declaring action's result type.</typeparam>
    /// <param name="actionId">The action this binding instance contributes to.</param>
    /// <param name="settingId">The fresh id this binding instance's setting is declared under.</param>
    /// <param name="initialValue">The setting's initial value.</param>
    /// <param name="factory">Builds a live binding instance from a settings value — invoked once now
    /// and again on every dirty re-resolution (D-22).</param>
    BindingInstanceHandle AddBinding<TSelf, TSetting, TResult>(
        Identification<TResult> actionId,
        Identification<TSetting> settingId,
        TSetting initialValue,
        Func<TSetting, TSelf> factory)
        where TSelf : struct, IBindingType<TSelf, TResult>;

    /// <summary>
    /// D-21 rebind verb: undeclares the binding instance's setting and detaches it from its action's
    /// evaluation set (a genuine <see cref="ISettingsManager.Undeclare"/> — every handle this can
    /// target is a user-added instance, RESEARCH Pattern 6).
    /// </summary>
    /// <param name="handle">A handle previously returned by <see cref="AddBinding{TSelf,TSetting,TResult}"/>.</param>
    void RemoveBinding(BindingInstanceHandle handle);

    /// <summary>
    /// D-21 rebind verb: writes <paramref name="newValue"/> through
    /// <see cref="ISettingsManager.SetUserValue{T}"/> — the substrate is the single source of truth.
    /// Composite value-type settings (D-17, R-6) rebind in ONE atomic write.
    /// </summary>
    /// <typeparam name="TSetting">The binding's settings value type.</typeparam>
    /// <param name="handle">A handle previously returned by <see cref="AddBinding{TSelf,TSetting,TResult}"/>.</param>
    /// <param name="newValue">The new settings value.</param>
    Result<SetError> Rebind<TSetting>(BindingInstanceHandle handle, TSetting newValue);

    /// <summary>
    /// D-23 conflict reverse-lookup: informational only, NEVER a veto. Returns every live
    /// <see cref="AddBinding{TSelf,TSetting,TResult}"/>-created binding of settings type
    /// <typeparamref name="TSetting"/> whose CURRENT value satisfies
    /// <paramref name="referencesSourceValue"/>.
    /// </summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    IReadOnlyList<Identification> FindBindingsReferencing<TSetting>(Func<TSetting, bool> referencesSourceValue);

    /// <summary>The inverse of <see cref="FindBindingsReferencing{TSetting}"/> (D-23): scoped to one action.</summary>
    /// <typeparam name="TSetting">The binding settings type to filter by.</typeparam>
    /// <param name="actionId">The action to scope the reverse-lookup to.</param>
    /// <param name="referencesSourceValue">Tests whether a live binding's current setting value
    /// references the source value of interest.</param>
    IReadOnlyList<Identification> FindBindingsOnActionReferencing<TSetting>(
        Identification actionId, Func<TSetting, bool> referencesSourceValue);
}

/// <summary>
/// State-facade marker for <see cref="IInputManager"/>. Exposes engine-internal per-frame/per-frame-enter
/// entry points — not part of the general mod-facing API, resolved only by the
/// <c>build_input_snapshot</c> and <c>establish_rebind_dirty_subscriptions</c> stateless functions.
/// </summary>
[FacadeFor<IInputManager>]
[PublicAPI]
public interface IInputManagerStateFacade
{
    /// <summary>
    /// Runs one per-frame snapshot pass (D-18): processes dirty bindings (D-22 entry point), bulk-fills
    /// registered source providers, evaluates bindings bunched by concrete type into per-action result
    /// slots with first-match-wins (D-19), then edge-detects on those slots and publishes via the 61.1
    /// event bus (D-25). Called once per frame by the engine's <c>build_input_snapshot</c> function.
    /// </summary>
    void BuildSnapshot();

    /// <summary>
    /// D-22/D-26/Pitfall-1: (re)establishes every live binding's rebind-dirty settings subscription,
    /// unconditionally, every time. Called once per frame-enter (including oscillation replay) by the
    /// engine's <c>establish_rebind_dirty_subscriptions</c> function.
    /// </summary>
    void EstablishRebindDirtySubscriptions();
}
