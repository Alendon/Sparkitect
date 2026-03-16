using JetBrains.Annotations;
using Sparkitect.DI.Container;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Strategy interface for metadata-driven service resolution.
/// Implementations interpret metadata entries to resolve services from the container.
/// A single provider instance is used per resolution scope; composition of multiple
/// strategies is the consumer's responsibility.
/// </summary>
[PublicAPI]
public interface IResolutionProvider
{
    /// <summary>
    /// Attempts to resolve a service using the provided metadata entries.
    /// </summary>
    /// <param name="serviceType">The type of service being requested.</param>
    /// <param name="container">The core container to resolve backing services from.</param>
    /// <param name="metadataEntries">The metadata entries associated with the requested service type.</param>
    /// <param name="service">The resolved service instance, or null if resolution failed.</param>
    /// <returns>True if the service was resolved successfully, false otherwise.</returns>
    bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service);
}
