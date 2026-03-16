using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Foundation resolution contract that all DI leaf code resolves against.
/// Provides a unified interface for service resolution regardless of the underlying
/// resolution strategy (container, metadata-driven provider, or custom implementation).
/// </summary>
[PublicAPI]
public interface IResolutionScope
{
    /// <summary>
    /// Attempts to resolve a service of the specified type within the context of the given wrapper type.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve.</typeparam>
    /// <param name="wrapperType">The wrapper/factory type performing the resolution, used as a metadata lookup key.</param>
    /// <param name="service">The resolved service instance, or null if not found.</param>
    /// <returns>True if the service was resolved successfully, false otherwise.</returns>
    bool TryResolve<T>(Type wrapperType, [NotNullWhen(true)] out T? service) where T : class;

    /// <summary>
    /// Attempts to resolve a service of the specified runtime type within the context of the given wrapper type.
    /// </summary>
    /// <param name="wrapperType">The wrapper/factory type performing the resolution, used as a metadata lookup key.</param>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <param name="service">The resolved service instance, or null if not found.</param>
    /// <returns>True if the service was resolved successfully, false otherwise.</returns>
    bool TryResolve(Type wrapperType, Type serviceType, out object? service);
}
