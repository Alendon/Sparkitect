using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[ModuleRegistry.RegisterModule("sample")]
public partial class SampleModule : IStateModule
{
    public static IReadOnlyList<Identification> RequiredModules => [StateModuleID.Sparkitect.Core];
    public static Identification Identification => StateModuleID.MinimalSampleMod.Sample;

    [StateFunction("process_registry")]
    [OnCreate]
    private static void ProcessRegistry(IRegistryManager registryManager)
    {
        registryManager.AddRegistry<DummyRegistry>();
        registryManager.ProcessAllMissing<DummyRegistry>();
    }

    [StateFunction("remove_registry")]
    [OnDestroy]
    private static void RemoveRegistry(IRegistryManager registryManager)
    {
        registryManager.UnregisterAllRemaining<DummyRegistry>();
    }
}