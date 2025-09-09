using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

/// <summary>
/// Core dependency injection container for resolving registered services and managing their lifetimes.
/// Provides type-safe service resolution with support for both required and optional dependencies.
/// </summary>
[PublicAPI]
public interface ICoreContainer : IDisposable
{
    /// <summary>
    /// Resolves a service of the specified type from the container.
    /// </summary>
    /// <typeparam name="TService">The type of service to resolve</typeparam>
    /// <returns>The resolved service instance</returns>
    /// <exception cref="DependencyResolutionException">Thrown when the service is not registered</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the container has been disposed</exception>
    TService Resolve<TService>() where TService : class;
    
    /// <summary>
    /// Attempts to resolve a service of the specified type from the container.
    /// </summary>
    /// <typeparam name="TService">The type of service to resolve</typeparam>
    /// <param name="service">The resolved service instance, or null if not found</param>
    /// <returns>True if the service was resolved successfully, false otherwise</returns>
    bool TryResolve<TService>([NotNullWhen(true)] out TService? service) where TService : class;
    
    /// <summary>
    /// Attempts to resolve a service of the specified type from the container.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve</param>
    /// <param name="service">The resolved service instance, or null if not found</param>
    /// <returns>True if the service was resolved successfully, false otherwise</returns>
    bool TryResolve(Type serviceType, out object? service);
    
    /// <summary>
    /// Gets a read-only dictionary of all registered service instances in this container.
    /// This does not include instances registered in parent containers
    /// </summary>
    /// <returns>A dictionary mapping service types to their instances</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the container has been disposed</exception>
    IReadOnlyDictionary<Type, object> GetCurrentRegisteredInstances();
}