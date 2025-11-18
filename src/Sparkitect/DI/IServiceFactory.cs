using Sparkitect.DI.Container;

namespace Sparkitect.DI;

/// <summary>
/// Interface for factories that create service instances with dependencies
/// </summary>
public interface IServiceFactory : IFactoryBase
{
    /// <summary>
    /// Gets the service type that this factory registers (typically an interface)
    /// </summary>
    Type ServiceType { get; }
    
    /// <summary>
    /// Creates an instance of the service using the provided container for resolving dependencies
    /// </summary>
    /// <param name="container">The container builder to resolve dependencies from</param>
    /// <param name="facadeMap">Facade-to-service type mappings for dependency resolution</param>
    /// <returns>The created service instance</returns>
    object CreateInstance(ICoreContainerBuilder container, IReadOnlyDictionary<Type, Type> facadeMap);

    /// <summary>
    /// Creates an instance of the service using the provided container for resolving dependencies
    /// </summary>
    /// <param name="container">The container builder to resolve dependencies from</param>
    /// <returns>The created service instance</returns>
    object CreateInstance(ICoreContainerBuilder container) => CreateInstance(container, new Dictionary<Type, Type>());

    /// <summary>
    /// Applies property dependencies to the created instance
    /// </summary>
    /// <param name="instance">The service instance to apply properties to</param>
    /// <param name="container">The container builder to resolve dependencies from</param>
    /// <param name="facadeMap">Facade-to-service type mappings for dependency resolution</param>
    void ApplyProperties(object instance, ICoreContainerBuilder container, IReadOnlyDictionary<Type, Type> facadeMap);

    /// <summary>
    /// Applies property dependencies to the created instance
    /// </summary>
    /// <param name="instance">The service instance to apply properties to</param>
    /// <param name="container">The container builder to resolve dependencies from</param>
    void ApplyProperties(object instance, ICoreContainerBuilder container) => ApplyProperties(instance, container, new Dictionary<Type, Type>());
}