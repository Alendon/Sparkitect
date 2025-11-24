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
