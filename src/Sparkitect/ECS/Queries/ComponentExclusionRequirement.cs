using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Capability requirement that rejects storages containing any of the excluded components.
/// Uses <c>!Overlaps</c> — the symmetric inverse of <see cref="ComponentSetRequirement"/>'s
/// <c>IsSubsetOf</c> check.
/// </summary>
public struct ComponentExclusionRequirement : ICapabilityRequirement<IChunkedIteration, ComponentSetMetadata>
{
    private readonly HashSet<Identification> _excludedComponents;

    public ComponentExclusionRequirement(IReadOnlyList<Identification> componentIds)
    {
        _excludedComponents = new HashSet<Identification>(componentIds);
    }

    public bool Matches(ComponentSetMetadata metadata)
    {
        return ComponentSetMatcher.ContainsNone(_excludedComponents, metadata.Components);
    }
}

/// <summary>
/// Generic capability requirement for rejecting storages that implement
/// <see cref="IChunkedIteration{TKey}"/> and contain any of the excluded components.
/// </summary>
public struct ComponentExclusionRequirement<TKey> : ICapabilityRequirement<IChunkedIteration<TKey>, ComponentSetMetadata>
    where TKey : unmanaged
{
    private readonly HashSet<Identification> _excludedComponents;

    public ComponentExclusionRequirement(IReadOnlyList<Identification> componentIds)
    {
        _excludedComponents = new HashSet<Identification>(componentIds);
    }

    public bool Matches(ComponentSetMetadata metadata)
    {
        return ComponentSetMatcher.ContainsNone(_excludedComponents, metadata.Components);
    }
}
