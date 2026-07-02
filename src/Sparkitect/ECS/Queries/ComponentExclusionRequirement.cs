using JetBrains.Annotations;
using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Capability requirement that rejects storages containing any of the excluded components.
/// Uses <c>!Overlaps</c> — the symmetric inverse of <see cref="ComponentSetRequirement"/>'s
/// <c>IsSubsetOf</c> check.
/// </summary>
[PublicAPI]
public struct ComponentExclusionRequirement : ICapabilityRequirement<IChunkedIteration, ComponentSetMetadata>
{
    private readonly HashSet<Identification> _excludedComponents;

    /// <summary>Creates the requirement from the component ids that must be absent.</summary>
    public ComponentExclusionRequirement(IReadOnlyList<Identification> componentIds)
    {
        _excludedComponents = new HashSet<Identification>(componentIds);
    }

    /// <summary>Returns true when the storage contains none of the excluded components.</summary>
    public bool Matches(ComponentSetMetadata metadata)
    {
        return ComponentSetMatcher.ContainsNone(_excludedComponents, metadata.Components);
    }
}

/// <summary>
/// Generic capability requirement for rejecting storages that implement
/// <see cref="IChunkedIteration{TKey}"/> and contain any of the excluded components.
/// </summary>
[PublicAPI]
public struct ComponentExclusionRequirement<TKey> : ICapabilityRequirement<IChunkedIteration<TKey>, ComponentSetMetadata>
    where TKey : unmanaged
{
    private readonly HashSet<Identification> _excludedComponents;

    /// <summary>Creates the requirement from the component ids that must be absent.</summary>
    public ComponentExclusionRequirement(IReadOnlyList<Identification> componentIds)
    {
        _excludedComponents = new HashSet<Identification>(componentIds);
    }

    /// <summary>Returns true when the storage contains none of the excluded components.</summary>
    public bool Matches(ComponentSetMetadata metadata)
    {
        return ComponentSetMatcher.ContainsNone(_excludedComponents, metadata.Components);
    }
}
