namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Non-generic base for capability requirements, enabling heterogeneous collections
/// (e.g., <c>IReadOnlyList&lt;ICapabilityRequirement&gt;</c>) of typed requirements.
/// </summary>
public interface ICapabilityRequirement
{
    /// <summary>
    /// Gets the capability interface type this requirement targets.
    /// Used as a fast pre-check before metadata matching.
    /// </summary>
    Type CapabilityType { get; }
}

/// <summary>
/// A typed capability requirement that matches against a specific capability interface
/// and metadata of type <typeparamref name="TMeta"/>.
/// Used by filters to determine which storages satisfy a query.
/// </summary>
/// <typeparam name="TCapability">
/// The capability interface type this requirement targets. Invariant (exact match only).
/// </typeparam>
/// <typeparam name="TMeta">
/// The metadata type this requirement matches against. Contravariant to allow
/// a requirement accepting a base metadata type to match derived metadata.
/// </typeparam>
public interface ICapabilityRequirement<TCapability, in TMeta> : ICapabilityRequirement
    where TCapability : ICapability
    where TMeta : ICapabilityMetadata
{
    /// <inheritdoc/>
    Type ICapabilityRequirement.CapabilityType => typeof(TCapability);

    /// <summary>
    /// Determines whether the given metadata satisfies this requirement.
    /// </summary>
    /// <param name="metadata">The capability metadata to evaluate.</param>
    /// <returns><c>true</c> if the metadata matches this requirement; otherwise <c>false</c>.</returns>
    bool Matches(TMeta metadata);
}
