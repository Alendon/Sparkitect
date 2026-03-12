using Sparkitect.Modding;

namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Shared capability metadata carrying the set of component <see cref="Identification"/> values
/// that a storage encodes. Used by both IChunkedIteration and component mutation capabilities
/// for query filter matching.
/// </summary>
/// <param name="Components">The set of component identifications present in the storage.</param>
public record ComponentSetMetadata(IReadOnlySet<Identification> Components) : ICapabilityMetadata;
