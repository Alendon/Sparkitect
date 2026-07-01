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
    /// Builds a resolution scope with additional metadata injected into every wrapper type.
    /// </summary>
    /// <param name="container">The core container for fallback resolution.</param>
    /// <param name="provider">Optional resolution provider for metadata-driven resolution.</param>
    /// <param name="modIds">Mod IDs to scan for metadata entrypoints.</param>
    /// <param name="wrapperTypes">The wrapper/factory types to collect metadata for.</param>
    /// <param name="supplementalMetadata">Additional metadata merged into every wrapper type's metadata dictionary.</param>
    /// <returns>A configured resolution scope.</returns>
    IResolutionScope BuildScope(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        IEnumerable<Type> wrapperTypes,
        Dictionary<Type, List<object>>? supplementalMetadata);

    /// <summary>
    /// Builds a complete factory container, owning the full pipeline: discovers configurator entrypoints,
    /// accumulates factory registrations from every configurator into a single aggregate map, extracts
    /// wrapper types, builds resolution scope, hands the finalized map to the stateless builder once, and
    /// returns the container.
    /// </summary>
    /// <typeparam name="TKey">The key type used to identify factories.</typeparam>
    /// <typeparam name="TBase">The base type for objects created by the factories.</typeparam>
    /// <param name="container">The core container for dependency resolution.</param>
    /// <param name="provider">Optional resolution provider for metadata-driven resolution.</param>
    /// <param name="modIds">Mod IDs to scan for configurator entrypoints.</param>
    /// <param name="configuratorEntrypointAttribute">The attribute type marking factory configurator entrypoints.</param>
    /// <param name="skipMissing">When true, factories whose dependencies cannot be resolved are silently dropped; otherwise an exception is thrown. Defaults to false.</param>
    /// <returns>A built factory container with all registered and prepared factories.</returns>
    IFactoryContainer<TKey, TBase> BuildFactoryContainer<TKey, TBase>(
        ICoreContainer container,
        IResolutionProvider? provider,
        IEnumerable<string> modIds,
        Type configuratorEntrypointAttribute,
        bool skipMissing = false)
        where TBase : class
        where TKey : notnull;

    /// <summary>
    /// Creates a child container builder whose parent chain points at <paramref name="parent"/>.
    /// Use when a subsystem needs an isolated container scope that still resolves shared services
    /// (Vulkan context, window, etc.) from the parent chain.
    /// </summary>
    /// <param name="parent">The parent container; the returned builder's services are layered above it.</param>
    ICoreContainerBuilder CreateChildContainerBuilder(ICoreContainer parent);

    /// <summary>
    /// Builds a child core container by discovering configurator entrypoints and letting each contribute
    /// registrations to a builder layered over <paramref name="parent"/>. The discovery attribute selects
    /// the configurator set; <paramref name="configure"/> invokes each configurator (and may filter).
    /// </summary>
    /// <typeparam name="TConfigurator">The non-generic configurator base to discover.</typeparam>
    /// <param name="parent">The parent container the built container layers over.</param>
    /// <param name="modIds">Mod IDs to scan for configurator entrypoints; also the loaded-mods set passed to each configurator.</param>
    /// <param name="discoveryAttribute">The attribute type marking configurator entrypoints.</param>
    /// <param name="configure">Callback invoked per configurator with the builder and loaded-mods set; applies any filtering and calls Configure.</param>
    ICoreContainer BuildConfiguredContainer<TConfigurator>(
        ICoreContainer parent,
        IEnumerable<string> modIds,
        Type discoveryAttribute,
        Action<TConfigurator, ICoreContainerBuilder, IReadOnlySet<string>> configure)
        where TConfigurator : class;
}
