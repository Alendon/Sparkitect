using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

/// <summary>
/// Builder for configuring and constructing core dependency injection containers.
/// Supports fluent service registration with validation and lifecycle management.
/// </summary>
[PublicAPI]
public interface ICoreContainerBuilder
{
    /// <summary>
    /// Registers a service factory in the container. The factory will be used to create service instances.
    /// </summary>
    /// <typeparam name="TServiceFactory">The service factory type that implements IServiceFactory</typeparam>
    /// <returns>The builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is already registered or exists in parent container</exception>
    ICoreContainerBuilder Register<TServiceFactory>() where TServiceFactory : IServiceFactory, new();
    
    /// <summary>
    /// Overrides an existing service registration with a new factory implementation.
    /// </summary>
    /// <typeparam name="TServiceFactory">The service factory type that implements IServiceFactory</typeparam>
    /// <returns>The builder instance for method chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not already registered</exception>
    ICoreContainerBuilder Override<TServiceFactory>() where TServiceFactory : IServiceFactory, new();
    
    /// <summary>
    /// Builds and validates the container, instantiating all registered services according to their dependency graph.
    /// </summary>
    /// <returns>A configured and ready-to-use core container</returns>
    /// <exception cref="CircularDependencyException">Thrown when circular dependencies are detected</exception>
    /// <exception cref="DependencyResolutionException">Thrown when required dependencies cannot be resolved</exception>
    ICoreContainer Build();
    
    /// <summary>
    /// Attempts to resolve a service during the container building phase, checking both current registrations and parent container.
    /// Used internally for dependency resolution validation.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve</typeparam>
    /// <param name="instance">The resolved service instance, or null if not found</param>
    /// <returns>True if the service was resolved successfully, false otherwise</returns>
    bool TryResolveInternal<T>([NotNullWhen(true)] out T? instance) where T : class;

    /// <summary>
    /// Attempts to resolve a service during the container building phase using a runtime type.
    /// Used internally for facade-mapped dependency resolution.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve</param>
    /// <param name="instance">The resolved service instance, or null if not found</param>
    /// <returns>True if the service was resolved successfully, false otherwise</returns>
    bool TryResolveInternal(Type serviceType, out object? instance);
}