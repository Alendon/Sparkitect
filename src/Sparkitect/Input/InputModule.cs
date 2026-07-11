using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Input;

/// <summary>
/// Device-agnostic state module providing the input action core: <see cref="ActionRegistry"/>
/// processing and the <see cref="IInputManager"/> service. Never references Windowing and never
/// names a device vocabulary (D-01). Declares its own independent edge onto the Event module
/// (C-5) to publish action edge events — never relies on another module's Event edge.
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("input")]
public sealed partial class InputModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires =>
        [StateModuleID.Sparkitect.Core, StateModuleID.Sparkitect.Event];

    /// <summary>Processes <see cref="ActionRegistry"/> registrations on state entry (D-26 registry precedent).</summary>
    [TransitionFunction("process_action_registry_up")]
    [OnFrameEnterScheduling]
    internal static void RegisterActions(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ActionRegistry, InputModule>();
    }

    /// <summary>Reverses <see cref="ActionRegistry"/> registrations on state exit (D-26 registry precedent).</summary>
    [TransitionFunction("process_action_registry_down")]
    [OnFrameExitScheduling]
    internal static void UnregisterActions(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<ActionRegistry, InputModule>();
    }

    /// <summary>
    /// (Re)establishes the rebind-dirty settings subscription for every live binding instance
    /// (D-21/D-22, Pitfall 1), UNCONDITIONALLY on every frame-enter — subscriptions are
    /// frame-token-scoped and swept wholesale on frame teardown, and
    /// <c>GameStateManager.TransitionToParent</c> re-runs the parent's enter methods on every
    /// pop/push oscillation (<c>GameStateManager.cs:768-773</c>). Reaches the manager only through
    /// the DI-resolved state-facade parameter (SPARK0401/0406 purity).
    /// </summary>
    [TransitionFunction("establish_rebind_dirty_subscriptions")]
    [OnFrameEnterScheduling]
    internal static void EstablishRebindDirtySubscriptions(IInputManagerStateFacade inputManager) =>
        inputManager.EstablishRebindDirtySubscriptions();

    /// <summary>
    /// The engine's second per-frame function (D-18): builds one input snapshot per frame -- dirty
    /// processing, type-bunched binding evaluation, first-match composition into per-action result
    /// slots, then edge-detect + publish. Reaches the manager only through the DI-resolved state-facade
    /// parameter (SPARK0401/0406 purity) -- the snapshot state itself lives in <see cref="InputManager"/>.
    /// </summary>
    [PerFrameFunction("build_input_snapshot")]
    [PerFrameScheduling]
    public static void BuildInputSnapshot(IInputManagerStateFacade inputManager) => inputManager.BuildSnapshot();
}
