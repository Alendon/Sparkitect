using System.Reflection;
using JetBrains.Annotations;
using Sparkitect.DI.Container;

namespace Sparkitect.DI;

/// <summary>
/// Service for DI-related operations on loaded mod assemblies
/// Handles entrypoint discovery, type scanning, and container creation
/// </summary>
[PublicAPI]
public interface IModDIService
{
    /// <summary>
    /// Registers assemblies for newly loaded mods
    /// Called by ModManager when mods are loaded
    /// </summary>
    /// <param name="modAssemblies">Dictionary of mod ID to Assembly</param>
    void RegisterModAssemblies(IReadOnlyDictionary<string, Assembly> modAssemblies);

    /// <summary>
    /// Unregisters mods that have been unloaded
    /// Called by ModManager when mods are unloaded
    /// </summary>
    /// <param name="modIds">List of mod IDs to unregister</param>
    void UnregisterMods(IReadOnlyList<string> modIds);

    /// <summary>
    /// Creates an entrypoint container for the specified mods
    /// </summary>
    /// <typeparam name="T">The base entrypoint type to discover</typeparam>
    /// <param name="modIds">Mod IDs to scan for entrypoints</param>
    /// <returns>A new entrypoint container with discovered entrypoints</returns>
    [MustDisposeResource]
    IEntrypointContainer<T> CreateEntrypointContainer<T>(IEnumerable<string> modIds)
        where T : class, IBaseConfigurationEntrypoint;
}
