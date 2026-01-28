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

    /// <summary>
    /// Resolves a service with type substitution via facade mapping. Used internally for facade injection.
    /// If <typeparamref name="TService"/> exists in <paramref name="map"/>, resolves the mapped type instead.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve.</typeparam>
    /// <param name="map">Type substitution map for facade resolution.</param>
    /// <returns>The resolved service instance.</returns>
    TService ResolveMapped<TService>(IReadOnlyDictionary<Type, Type> map) where TService : class
    {
        if (map.TryGetValue(typeof(TService), out var mappedType))
        {
            TryResolve(mappedType, out var service);
            return (service as TService)!;
        }

        return Resolve<TService>();
    }

    /// <summary>
    /// Attempts to resolve a service with type substitution via facade mapping. Used internally for facade injection.
    /// </summary>
    /// <typeparam name="TService">The service type to resolve.</typeparam>
    /// <param name="service">The resolved service instance, or null if not found.</param>
    /// <param name="map">Type substitution map for facade resolution.</param>
    /// <returns>True if the service was resolved successfully.</returns>
    bool TryResolveMapped<TService>([NotNullWhen(true)] out TService? service, IReadOnlyDictionary<Type, Type> map)
        where TService : class
    {
        if (map.TryGetValue(typeof(TService), out var mappedType) && TryResolve(mappedType, out var untypedService) && untypedService is TService tService)
        {
            service = tService;
            return true;
        }

        return TryResolve(out service);
    }

    /// <summary>
    /// Attempts to resolve a service with type substitution via facade mapping. Used internally for facade injection.
    /// </summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <param name="service">The resolved service instance, or null if not found.</param>
    /// <param name="map">Type substitution map for facade resolution.</param>
    /// <returns>True if the service was resolved successfully.</returns>
    bool TryResolveMapped(Type serviceType, out object? service, IReadOnlyDictionary<Type, Type> map)
    {
        if (map.TryGetValue(serviceType, out var mappedType) && TryResolve(mappedType, out service) && (service?.GetType().IsAssignableTo(serviceType) ?? false))
        {
            return true;
        }

        return TryResolve(serviceType, out service);
    }
}