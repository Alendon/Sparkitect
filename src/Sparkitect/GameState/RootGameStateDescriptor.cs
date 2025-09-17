using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

[StateDescriptionRegistry.RegisterStateAbc("root")]
public partial class RootGameStateDescriptor : IStateDescriptor
{
    public static Identification ParentId => Identification.Empty;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.Sparkitect.Core];

    [Transition(TransitionTrigger.Add)]
    internal static void LoadRootMods(IModManager modManager)
    {
        Log.Debug("Discovering mods");
        modManager.DiscoverMods();

        var modIds = modManager.DiscoveredArchives.Select(a => a.Id).ToArray();
        Log.Information("Loading {ModCount} mods", modIds.Length);

        modManager.LoadMods(modIds);
        Log.Debug("Mods loaded successfully");
    }

    [Transition(TransitionTrigger.Add)]
    [OrderAfter<LoadRootModsMethod>]
    internal static void ProcessRegistries(IRegistryManagerFacade registryManager)
    {
        Log.Debug("Processing registries");
        registryManager.ProcessRegistry();
        Log.Debug("Registries processed successfully");
    }
}


/*
 * This part would be Source Generated
*/


public partial class RootGameStateDescriptor : IStateDescriptorMethods
{
    public class LoadRootModsMethod : IStateMethod
    {
        private IModManager _modManager = null!;
        
        public void Execute()
        {
            LoadRootMods(_modManager);
        }

        public void Initialize(IStateContainer container)
        {
            _modManager = container.Resolve<IModManager>();
        }
    }
    
    public class ProcessRegistriesMethod : IStateMethod
    {
        private IRegistryManagerFacade _facade = null!;
        
        public void Execute()
        {
            ProcessRegistries(_facade);
        }

        public void Initialize(IStateContainer container)
        {
            _facade = container.Resolve<IRegistryManagerFacade>();
        }
    }

    public IReadOnlyList<IStateMethod> ContainingMethods => [new LoadRootModsMethod(), new ProcessRegistriesMethod()];
}