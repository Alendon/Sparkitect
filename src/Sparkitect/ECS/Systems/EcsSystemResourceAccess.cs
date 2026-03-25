using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Metadata type carrying aggregated read/write component access for a single ECS system.
/// Populated by SG from query parameter component IDs. Consumed by debug tooling and
/// future concurrent graph builder.
/// </summary>
public sealed class EcsSystemResourceAccess
{
    public IReadOnlySet<Identification> ReadComponentIds { get; }
    public IReadOnlySet<Identification> WriteComponentIds { get; }

    public EcsSystemResourceAccess(
        IReadOnlySet<Identification> readComponentIds,
        IReadOnlySet<Identification> writeComponentIds)
    {
        ReadComponentIds = readComponentIds;
        WriteComponentIds = writeComponentIds;
    }
}
