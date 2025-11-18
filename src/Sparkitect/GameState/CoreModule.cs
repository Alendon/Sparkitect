using JetBrains.Annotations;
using Serilog;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

[PublicAPI]
[ModuleRegistry.RegisterModule("core")]
public sealed partial class CoreModule : IStateModule
{
    public const string Key_LoadRootMods = "load_root_mods";
    public const string Key_RegisterNewStates = "register_new_states";
    public const string Key_UnloadRootMods = "unload_root_mods";
    public const string Key_UnregisterStates = "unregister_states";
    
    public static IReadOnlyList<Type> UsedServices =>
    [
        typeof(IModManager),
        typeof(ICliArgumentHandler),
        typeof(IGameStateManager),
        typeof(IIdentificationManager),
        typeof(IRegistryManager)
    ];

    public static Identification Identification => StateModuleID.Sparkitect.Core;


    /*
     * In future here will be additionall Feature and Transitions placed
     * These can be referenced by the developers to be included into their Game State
     * For example a Physics Module can declare all Transitions and Features it needs to function
     * With this a developer can just put together their Game States
     */


    [StateFunction(Key_LoadRootMods)]
    [OnModuleEnter]
    internal static void LoadRootMods(IModManager modManager)
    {
        Log.Debug("Discovering mods");
        modManager.DiscoverMods();

        var modIds = modManager.DiscoveredArchives.Select(a => a.Id).ToArray();
        Log.Information("Loading {ModCount} mods", modIds.Length);

        modManager.LoadMods(modIds);
        Log.Debug("Mods loaded successfully");
    }

    [OnStateEnter]
    [OrderAfter(Key_LoadRootMods)]
    [StateFunction(Key_RegisterNewStates)]
    internal static void ProcessStateRegistry(IRegistryManager registryManager)
    {
        //TODO trigger ModuleRegistry and StateRegistry
        throw new NotImplementedException();
    }

    [StateFunction(Key_UnregisterStates)]
    [OnStateExit]
    [OrderBefore(Key_UnloadRootMods)]
    internal static void UnregisterStates(IRegistryManager registryManager)
    {
        throw new NotImplementedException();
    }

    [StateFunction(Key_UnloadRootMods)]
    [OnStateExit]
    [OrderAfter(Key_UnregisterStates)]
    internal static void UnloadRootMods(IModManager modManager)
    {
        throw new NotImplementedException();
    }
    
    
}