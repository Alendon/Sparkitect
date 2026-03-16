using JetBrains.Annotations;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Non-generic base contract for contributing resolution metadata.
/// Implementations populate the dependency metadata dictionary that drives
/// metadata-aware resolution via <see cref="IResolutionProvider"/>.
/// </summary>
[PublicAPI]
public interface IResolutionMetadataEntrypoint
{
    /// <summary>
    /// Populates the dependency metadata dictionary with entries for this entrypoint's wrapper type.
    /// </summary>
    /// <param name="dependencies">
    /// The inner metadata dictionary mapping dependency types to their metadata entry lists.
    /// Implementations should add entries for each dependency they manage.
    /// </param>
    void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies);
}

/// <summary>
/// Generic metadata entrypoint variant that associates the entrypoint with a specific wrapper type
/// via <typeparamref name="TWrapperType"/>. Used for attribute-based discovery with
/// <see cref="ResolutionMetadataEntrypointAttribute{TWrapperType}"/>.
/// </summary>
/// <typeparam name="TWrapperType">The wrapper/factory type this entrypoint provides metadata for.</typeparam>
[PublicAPI]
public interface IResolutionMetadataEntrypoint<TWrapperType> : IResolutionMetadataEntrypoint;
