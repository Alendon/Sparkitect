using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Reusable capability requirement for matching storages that contain a set of components.
/// Internalizes the pattern from MinimalSampleMod's InteractionCapability.
/// </summary>
public struct ComponentSetRequirement : ICapabilityRequirement<IChunkedIteration, ComponentSetMetadata>
{
    private readonly HashSet<Identification> _requiredComponents;

    /// <summary>
    /// Creates a requirement that matches storages containing all specified components.
    /// </summary>
    /// <param name="componentIds">The component identifications that must all be present.</param>
    public ComponentSetRequirement(IReadOnlyList<Identification> componentIds)
    {
        _requiredComponents = new HashSet<Identification>(componentIds);
    }

    /// <summary>
    /// Returns true when the storage's component set is a superset of the required components.
    /// </summary>
    public bool Matches(ComponentSetMetadata metadata)
    {
        return _requiredComponents.IsSubsetOf(metadata.Components);
    }
}
