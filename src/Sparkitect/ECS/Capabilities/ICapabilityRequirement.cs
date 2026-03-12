namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Non-generic base for capability requirements, enabling heterogeneous collections
/// (e.g., <c>IReadOnlyList&lt;ICapabilityRequirement&gt;</c>) of typed requirements.
/// </summary>
public interface ICapabilityRequirement;

/// <summary>
/// A typed capability requirement that matches against metadata of type <typeparamref name="TMeta"/>.
/// Used by filters to determine which storages satisfy a query.
/// </summary>
/// <typeparam name="TMeta">
/// The metadata type this requirement matches against. Contravariant to allow
/// a requirement accepting a base metadata type to match derived metadata.
/// </typeparam>
public interface ICapabilityRequirement<in TMeta> : ICapabilityRequirement
    where TMeta : ICapabilityMetadata
{
    /// <summary>
    /// Determines whether the given metadata satisfies this requirement.
    /// </summary>
    /// <param name="metadata">The capability metadata to evaluate.</param>
    /// <returns><c>true</c> if the metadata matches this requirement; otherwise <c>false</c>.</returns>
    bool Matches(TMeta metadata);
}
