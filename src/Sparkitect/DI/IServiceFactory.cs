using Sparkitect.DI.Resolution;

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
    /// Creates an instance of the service using the provided resolution scope for resolving dependencies
    /// </summary>
    /// <param name="scope">The resolution scope to resolve dependencies from</param>
    /// <returns>The created service instance</returns>
    object CreateInstance(IResolutionScope scope);

    /// <summary>
    /// Applies property dependencies to the created instance
    /// </summary>
    /// <param name="instance">The service instance to apply properties to</param>
    /// <param name="scope">The resolution scope to resolve dependencies from</param>
    void ApplyProperties(object instance, IResolutionScope scope);
}
