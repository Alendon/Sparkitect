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
/// Configuration entrypoint for factory registration. Implementations register keyed factories
/// with an <see cref="IFactoryContainerBuilder{TBase}"/> during initialization.
/// </summary>
/// <typeparam name="TBase">The base type for objects created by the factories.</typeparam>
/// <typeparam name="TDiscoveryAttribute">The attribute type used to discover implementations of this entrypoint.</typeparam>
public interface IFactoryConfigurator<TBase, TDiscoveryAttribute>
    : IConfigurationEntrypoint<TDiscoveryAttribute>
    where TBase : class
    where TDiscoveryAttribute : Attribute
{
    /// <summary>
    /// Configures keyed factories with the factory container builder.
    /// </summary>
    /// <param name="builder">The factory container builder to register factories with.</param>
    /// <param name="loadedMods">The set of currently loaded mod IDs.</param>
    void Configure(IFactoryContainerBuilder<TBase> builder, IReadOnlySet<string> loadedMods);
}
