using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Metadata type carrying aggregated read/write component access for a single ECS system.
/// Populated by SG from query parameter component IDs. Consumed by debug tooling and
/// future concurrent graph builder.
/// </summary>
[PublicAPI]
public sealed class EcsSystemResourceAccess
{
    /// <summary>Component ids the system reads.</summary>
    public IReadOnlySet<Identification> ReadComponentIds { get; }

    /// <summary>Component ids the system writes.</summary>
    public IReadOnlySet<Identification> WriteComponentIds { get; }

    /// <summary>Creates the access record from the aggregated read and write component id sets.</summary>
    public EcsSystemResourceAccess(
        IReadOnlySet<Identification> readComponentIds,
        IReadOnlySet<Identification> writeComponentIds)
    {
        ReadComponentIds = readComponentIds;
        WriteComponentIds = writeComponentIds;
    }
}
