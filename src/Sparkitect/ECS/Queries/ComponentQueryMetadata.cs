using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Concrete query parameter metadata for <see cref="ComponentQuery"/>.
/// Creates the query instance, registers a reactive filter with the world,
/// and carries the component IDs for iteration.
/// </summary>
public class ComponentQueryMetadata : QueryParameterMetadata
{
    private readonly IReadOnlyList<Identification> _componentIds;

    /// <summary>
    /// Creates metadata for a ComponentQuery targeting the specified components.
    /// </summary>
    /// <param name="componentIds">The component identifications to query.</param>
    public ComponentQueryMetadata(IReadOnlyList<Identification> componentIds)
    {
        _componentIds = componentIds;
    }

    /// <inheritdoc/>
    public override object CreateQuery(IWorld world)
    {
        ICapabilityRequirement[] filter = [new ComponentSetRequirement(_componentIds)];

        List<StorageHandle> matchedStorages = new();
        var filterHandle = world.RegisterFilter(filter, storages =>
        {
            matchedStorages.Clear();
            matchedStorages.AddRange(storages);
        });

        return new ComponentQuery(world, _componentIds, matchedStorages, filterHandle);
    }

    /// <inheritdoc/>
    public override void DisposeQuery(object query)
    {
        ((ComponentQuery)query).Dispose();
    }
}

/// <summary>
/// Generic query parameter metadata for <see cref="ComponentQuery{TKey}"/>.
/// Creates keyed queries that match storages implementing <see cref="Capabilities.IChunkedIteration{TKey}"/>.
/// </summary>
public class ComponentQueryMetadata<TKey> : QueryParameterMetadata
    where TKey : unmanaged
{
    private readonly IReadOnlyList<Identification> _componentIds;

    /// <summary>
    /// Creates metadata for a keyed ComponentQuery targeting the specified components.
    /// </summary>
    /// <param name="componentIds">The component identifications to query.</param>
    public ComponentQueryMetadata(IReadOnlyList<Identification> componentIds)
    {
        _componentIds = componentIds;
    }

    /// <inheritdoc/>
    public override object CreateQuery(IWorld world)
    {
        ICapabilityRequirement[] filter = [new ComponentSetRequirement<TKey>(_componentIds)];

        List<StorageHandle> matchedStorages = new();
        var filterHandle = world.RegisterFilter(filter, storages =>
        {
            matchedStorages.Clear();
            matchedStorages.AddRange(storages);
        });

        return new ComponentQuery<TKey>(world, _componentIds, matchedStorages, filterHandle);
    }

    /// <inheritdoc/>
    public override void DisposeQuery(object query)
    {
        ((ComponentQuery<TKey>)query).Dispose();
    }
}
