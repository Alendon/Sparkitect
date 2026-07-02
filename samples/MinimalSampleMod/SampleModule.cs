using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;

namespace MinimalSampleMod;

[ModuleRegistry.RegisterModule("sample")]
public partial class SampleModule : IStateModule, IHasIdentification
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];

    [TransitionFunction("process_registry_enter")]
    [OnFrameEnterScheduling]
    private static void ProcessRegistryEnter(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<DummyRegistry, SampleModule>();
    }

    [TransitionFunction("process_registry_exit")]
    [OnFrameExitScheduling]
    private static void ProcessRegistryExit(IRegistryManager registryManager)
    {
        registryManager.ProcessRegistry<DummyRegistry, SampleModule>();
    }
}