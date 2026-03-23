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

        return CreateQueryInstance(world, _componentIds, matchedStorages, filterHandle);
    }

    /// <summary>
    /// Creates the query instance. Override in <see cref="ComponentQueryMetadata{TQuery,TKey}"/>
    /// to construct typed subclasses.
    /// </summary>
    protected virtual ComponentQuery<TKey> CreateQueryInstance(
        IWorld world, IReadOnlyList<Identification> componentIds,
        List<StorageHandle> matchedStorages, FilterHandle filterHandle)
    {
        return new ComponentQuery<TKey>(world, componentIds, matchedStorages, filterHandle);
    }

    /// <inheritdoc/>
    public override void DisposeQuery(object query)
    {
        ((ComponentQuery<TKey>)query).Dispose();
    }
}

/// <summary>
/// Typed query parameter metadata that creates a specific <typeparamref name="TQuery"/> subclass
/// of <see cref="ComponentQuery{TKey}"/>. Used for named query types (e.g., BulletQuery, EnemyQuery)
/// that enable multiple distinct query parameters of the same base type on a single system.
/// </summary>
public class ComponentQueryMetadata<TQuery, TKey> : ComponentQueryMetadata<TKey>
    where TQuery : ComponentQuery<TKey>
    where TKey : unmanaged
{
    private readonly Func<IWorld, IReadOnlyList<Identification>, List<StorageHandle>, FilterHandle, TQuery> _factory;

    /// <summary>
    /// Creates metadata for a typed keyed ComponentQuery targeting the specified components.
    /// </summary>
    /// <param name="componentIds">The component identifications to query.</param>
    /// <param name="factory">Factory delegate that constructs the specific query subclass.</param>
    public ComponentQueryMetadata(
        IReadOnlyList<Identification> componentIds,
        Func<IWorld, IReadOnlyList<Identification>, List<StorageHandle>, FilterHandle, TQuery> factory)
        : base(componentIds)
    {
        _factory = factory;
    }

    /// <inheritdoc/>
    protected override ComponentQuery<TKey> CreateQueryInstance(
        IWorld world, IReadOnlyList<Identification> componentIds,
        List<StorageHandle> matchedStorages, FilterHandle filterHandle)
    {
        return _factory(world, componentIds, matchedStorages, filterHandle);
    }
}
