using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Interface for building and configuring IoC container registries.
/// Provides an entrypoint for mods and components to register their services with the dependency injection system.
/// </summary>
public interface IIoCRegistryBuilder : ConfigurationEntrypoint<IoCRegistryBuilderEntrypointAttribute>
{
    /// <summary>
    /// Configures registries for dependency injection using the provided registry proxy.
    /// This method is called during container initialization to register mod-specific services.
    /// </summary>
    /// <param name="registryProxy">The proxy used to register services and components with the DI container</param>
    void ConfigureRegistries(IRegistryProxy registryProxy);
}

/// <summary>
/// Proxy interface for registering services and components with the dependency injection container.
/// Provides a controlled interface for mods to add their registries without direct container access.
/// </summary>
public interface IRegistryProxy
{
    /// <summary>
    /// Adds a registry of the specified type to the dependency injection container.
    /// </summary>
    /// <typeparam name="TRegistry">The type of registry to add, must implement IRegistry</typeparam>
    /// <param name="categoryIdentifier">The category identifier for organizing and namespacing the registry</param>
    void AddRegistry<TRegistry>(string categoryIdentifier) where TRegistry : IRegistry;
}