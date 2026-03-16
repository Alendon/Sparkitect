using System.Reflection;
using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;

namespace Sparkitect.DI;

/// <summary>
/// Service for DI-related operations on loaded mod assemblies.
/// Handles entrypoint discovery, type scanning, container creation,
/// resolution scope building, and factory container creation.
/// </summary>
[PublicAPI]
public interface IDIService
{
    /// <summary>
    /// Registers assemblies for newly loaded mods.
    /// Called by ModManager when mods are loaded.
    /// </summary>
    /// <param name="modAssemblies">Dictionary of mod ID to Assembly.</param>
    void RegisterModAssemblies(IReadOnlyDictionary<string, Assembly> modAssemblies);

    /// <summary>
    /// Unregisters mods that have been unloaded.
    /// Called by ModManager when mods are unloaded.
    /// </summary>
    /// <param name="modIds">List of mod IDs to unregister.</param>
    void UnregisterMods(IReadOnlyList<string> modIds);

    /// <summary>
    /// Creates an entrypoint container for the specified mods.
    /// </summary>
    /// <typeparam name="T">The base entrypoint type to discover.</typeparam>
    /// <param name="modIds">Mod IDs to scan for entrypoints.</param>
    /// <returns>A new entrypoint container with discovered entrypoints.</returns>
    [MustDisposeResource]
    IEntrypointContainer<T> CreateEntrypointContainer<T>(IEnumerable<string> modIds)
        where T : class, IBaseConfigurationEntrypoint;

    /// <summary>
    /// Creates an entrypoint container using a runtime-provided attribute type for discovery.
    /// Enables metadata entrypoint collection via MakeGenericType without reflection on the generic method.
    /// </summary>
    /// <typeparam name="T">The base entrypoint type to discover.</typeparam>
    /// <param name="modIds">Mod IDs to scan for entrypoints.</param>
    /// <param name="entrypointAttribute">The attribute type used for entrypoint discovery.</param>
    /// <returns>A new entrypoint container with discovered entrypoints.</returns>
    [MustDisposeResource]
    IEntrypointContainer<T> CreateEntrypointContainer<T>(IEnumerable<string> modIds, Type entrypointAttribute)
        where T : class;

    /// <summary>
    /// Builds a resolution scope with metadata collection for the specified wrapper types.
    /// Discovers metadata entrypoints for each wrapper type and assembles the metadata dictionary.
    /// </summary>
    /// <param name="container">The core container for fallback resolution.</param>
    /// <param name="provider">Optional resolution provider for metadata-driven resolution.</param>
    /// <param name="modIds">Mod IDs to scan for metadata entrypoints.</param>
    /// <param name="wrapperTypes">The wrapper/factory types to collect metadata for.</param>
    /// <returns>A configured resolution scope.</returns>
    IResolutionScope BuildScope(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        IEnumerable<Type> wrapperTypes);

    /// <summary>
    /// Builds a complete factory container, owning the full pipeline:
    /// discovers configurator entrypoints, collects factory registrations,
    /// extracts wrapper types, builds resolution scope, prepares factories, and returns the container.
    /// </summary>
    /// <typeparam name="TBase">The base type for objects created by the factories.</typeparam>
    /// <param name="container">The core container for dependency resolution.</param>
    /// <param name="provider">Optional resolution provider for metadata-driven resolution.</param>
    /// <param name="modIds">Mod IDs to scan for configurator entrypoints.</param>
    /// <param name="configuratorEntrypointAttribute">The attribute type marking factory configurator entrypoints.</param>
    /// <returns>A built factory container with all registered and prepared factories.</returns>
    IFactoryContainer<TBase> BuildFactoryContainer<TBase>(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        Type configuratorEntrypointAttribute)
        where TBase : class;
}
