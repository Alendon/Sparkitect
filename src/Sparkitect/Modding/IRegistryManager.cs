using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.GameState;

namespace Sparkitect.Modding;

/// <summary>
/// Manages registry generation and tracks which mods are processed per registry.
/// </summary>
[PublicAPI]
public interface IRegistryManager
{
    /// <summary>
    /// Populates or tears down a registry at a state-transition hook. Called at both
    /// <c>[OnFrameEnter]</c> and <c>[OnFrameExit]</c>; the manager auto-detects the direction from the
    /// game-state manager (enter populates missing mods, exit reverses the snapshot) and throws if no
    /// transition is active.
    /// </summary>
    /// <typeparam name="TRegistry">The registry type.</typeparam>
    /// <typeparam name="TModule">The registry's owning module, matching <c>IRegistry&lt;TModule&gt;</c>.</typeparam>
    void ProcessRegistry<TRegistry, TModule>()
        where TRegistry : class, IRegistry<TModule>
        where TModule : IHasIdentification, IStateModule;

    /// <summary>
    /// Gets all active registry identifiers (added instances).
    /// </summary>
    IEnumerable<string> GetActiveRegistries();

    /// <summary>
    /// Gets the mod IDs processed for a specific registry.
    /// </summary>
    /// <typeparam name="TRegistry">The registry type to query.</typeparam>
    IEnumerable<string> GetProcessedMods<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Checks whether a registry instance has been added.
    /// </summary>
    /// <typeparam name="TRegistry">The registry type to check.</typeparam>
    bool IsRegistryActive<TRegistry>() where TRegistry : class, IRegistry;
}

/// <summary>
/// Internal composite seam the game-state manager drives: registry instances are added when their owning
/// module is created and removed when it is destroyed, all keyed by the <c>IRegistry&lt;TModule&gt;</c>
/// owning-module link. Instance add/remove is bookkeeping only and never touches native resources.
/// </summary>
internal interface IRegistryLifecycleManager
{
    /// <summary>Adds the instances of every registry owned by <paramref name="moduleId"/>.</summary>
    void AddModuleRegistries(Identification moduleId, ICoreContainer container);

    /// <summary>Removes the tracked instances of every registry owned by <paramref name="moduleId"/>.</summary>
    void RemoveModuleRegistries(Identification moduleId);

    /// <summary>Populates the registries owned by <paramref name="moduleId"/> for a specific mod set.</summary>
    void ProcessModuleRegistriesForMods(Identification moduleId, IReadOnlyList<string> modIds, ICoreContainer container);

    /// <summary>
    /// Root-entry bootstrap: adds and populates every currently-resolvable registry (CoreModule's four) for
    /// the root mods before the finalize pass, so states and modules are registered when it validates them.
    /// </summary>
    void BootstrapRootRegistries(IReadOnlyList<string> modIds, ICoreContainer container);
}
