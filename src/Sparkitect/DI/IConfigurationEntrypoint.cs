using Sparkitect.DI.Container;

namespace Sparkitect.DI;

/// <summary>
/// Base interface for all configuration entrypoints. Entrypoints are discovered classes that configure
/// various engine/mod components during initialization.
/// </summary>
public interface IBaseConfigurationEntrypoint
{
    /// <summary>
    /// Gets the attribute type that marks classes for discovery as this entrypoint type.
    /// </summary>
    public static abstract Type EntrypointAttributeType { get; }
}

/// <summary>
/// Generic configuration entrypoint interface that associates an entrypoint with its discovery attribute.
/// Implementations must have parameterless constructors and are discovered across all loaded mods.
/// </summary>
/// <typeparam name="TDiscoveryAttribute">The attribute type used to discover implementations of this entrypoint.</typeparam>
public interface IConfigurationEntrypoint<TDiscoveryAttribute> : IBaseConfigurationEntrypoint where TDiscoveryAttribute : Attribute
{
    static Type IBaseConfigurationEntrypoint.EntrypointAttributeType => typeof(TDiscoveryAttribute);
}

/// <summary>
/// Configuration entrypoint for core service registration. Implementations register services
/// with an <see cref="ICoreContainerBuilder"/> during initialization.
/// </summary>
/// <typeparam name="TDiscoveryAttribute">The attribute type used to discover implementations of this entrypoint.</typeparam>
public interface ICoreConfigurator<TDiscoveryAttribute>
    : IConfigurationEntrypoint<TDiscoveryAttribute>
    where TDiscoveryAttribute : Attribute
{
    /// <summary>
    /// Configures core services with the container builder.
    /// </summary>
    /// <param name="builder">The container builder to register services with.</param>
    /// <param name="loadedMods">The set of currently loaded mod IDs.</param>
    void Configure(ICoreContainerBuilder builder, IReadOnlySet<string> loadedMods);
}

/// <summary>
/// Non-generic bridge interface for factory configurators.
/// Enables <see cref="IDIService.BuildFactoryContainer{TKey,TBase}"/> to discover configurators
/// with the relaxed <c>CreateEntrypointContainer</c> overload and call Configure through the base interface.
/// Configurators write directly into the shared aggregate registration map; DIService owns the map and
/// hands the finalized map to <see cref="IFactoryContainerBuilder{TKey,TBase}.Build"/> once after every
/// configurator has contributed.
/// </summary>
/// <typeparam name="TKey">The key type used to identify factories.</typeparam>
/// <typeparam name="TBase">The base type for objects created by the factories.</typeparam>
public interface IFactoryConfiguratorBase<TKey, TBase>
    where TBase : class
    where TKey : notnull
{
    /// <summary>
    /// Writes keyed factory registrations into the shared aggregate map. Later writes silently
    /// override earlier writes for the same key (later-wins).
    /// </summary>
    /// <param name="registrations">The shared aggregate map to write factory registrations into.</param>
    /// <param name="loadedMods">The set of currently loaded mod IDs.</param>
    void Configure(IDictionary<TKey, IKeyedFactory<TBase>> registrations, IReadOnlySet<string> loadedMods);
}

/// <summary>
/// Configuration entrypoint for factory registration. Implementations write keyed factory registrations
/// into a shared aggregate map; DIService finalizes the map and builds the container once.
/// </summary>
/// <typeparam name="TKey">The key type used to identify factories.</typeparam>
/// <typeparam name="TBase">The base type for objects created by the factories.</typeparam>
/// <typeparam name="TDiscoveryAttribute">The attribute type used to discover implementations of this entrypoint.</typeparam>
public interface IFactoryConfigurator<TKey, TBase, TDiscoveryAttribute>
    : IConfigurationEntrypoint<TDiscoveryAttribute>, IFactoryConfiguratorBase<TKey, TBase>
    where TBase : class
    where TKey : notnull
    where TDiscoveryAttribute : Attribute;
