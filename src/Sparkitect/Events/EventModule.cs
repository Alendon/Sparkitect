using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace Sparkitect.Events;

/// <summary>
/// Opt-in state module providing the event bus infrastructure. States that need events compose this
/// module; the event manager service is exposed via <c>[StateService]</c>.
/// </summary>
[PublicAPI]
[ModuleRegistry.RegisterModule("event")]
public sealed partial class EventModule : TransitiveStateModule, IHasIdentification
{
    /// <inheritdoc />
    public override IReadOnlyList<Identification> Requires => [StateModuleID.Sparkitect.Core];

    [TransitionFunction("process_event_registry_up")]
    [OnFrameEnterScheduling]
    internal static void RegisterEvents(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<EventRegistry, EventModule>();
    }
    
    [TransitionFunction("process_event_registry_down")]
    [OnFrameExitScheduling]
    internal static void UnregisterEvents(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<EventRegistry, EventModule>();
    }
}
