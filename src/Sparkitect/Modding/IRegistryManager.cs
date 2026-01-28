using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Manages registry processing and tracks which mods are registered per registry
/// </summary>
[PublicAPI]
public interface IRegistryManager
{
    /// <summary>
    /// Process a specific registry for the given mods
    /// </summary>
    void ProcessRegistry<TRegistry>(IReadOnlyList<string> modIds) where TRegistry : class, IRegistry;

    /// <summary>
    /// Process all currently loaded mods that have not yet been processed for the given registry
    /// </summary>
    void ProcessAllMissing<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Unregister all mods currently processed for the given registry
    /// </summary>
    void UnregisterAllRemaining<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Adds a registry type to be managed. Must be called before processing registrations.
    /// </summary>
    void AddRegistry<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Gets all active registry types that have been added.
    /// </summary>
    /// <returns>An enumerable of registry type names.</returns>
    IEnumerable<string> GetActiveRegistries();

    /// <summary>
    /// Gets the mod IDs that have been processed for a specific registry.
    /// </summary>
    /// <typeparam name="TRegistry">The registry type to query.</typeparam>
    /// <returns>An enumerable of mod ID strings that have been processed, or empty if registry not active.</returns>
    IEnumerable<string> GetProcessedMods<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Checks if a registry type has been added and is active.
    /// </summary>
    /// <typeparam name="TRegistry">The registry type to check.</typeparam>
    /// <returns><c>true</c> if the registry is active; otherwise, <c>false</c>.</returns>
    bool IsRegistryActive<TRegistry>() where TRegistry : class, IRegistry;

    /// <summary>
    /// Gets whether registry mutations are currently expected.
    /// Returns true during ProcessRegistry calls, false otherwise.
    /// </summary>
    bool IsMutationExpected { get; }
}