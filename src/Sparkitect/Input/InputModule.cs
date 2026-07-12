using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Input;

/// <summary>
/// Device-agnostic state module providing the input action core: <see cref="ActionRegistry"/>
/// identity + fan-out registration only. Never references any input implementation (D-01/D-02)
/// and carries no per-frame function — sampling, evaluation, and push/pull dispatch are entirely
/// implementation-owned (e.g. <c>WindowInput</c>'s <c>WindowInputModule</c>).
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("input")]
public sealed partial class InputModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc/>
    public override IReadOnlyList<Identification> Requires =>
        [StateModuleID.Sparkitect.Event];

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
    /// Per-frame ordering anchor, deliberately a no-op: input implementations order their
    /// processing BEFORE this function, consumers order their simulation AFTER it — so a mod
    /// ticking on processed input never names any implementation module.
    /// </summary>
    [PerFrameFunction("input_processed")]
    [PerFrameScheduling]
    public static void InputProcessed()
    {
    }
}
