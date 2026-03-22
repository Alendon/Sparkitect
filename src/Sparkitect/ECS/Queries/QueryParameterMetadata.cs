namespace Sparkitect.ECS.Queries;

/// <summary>
/// Abstract base class for query parameter metadata. The EcsResolutionProvider resolves
/// against this type -- concrete implementations create the query instance and register
/// filter callbacks with the World.
/// </summary>
public abstract class QueryParameterMetadata
{
    /// <summary>
    /// Creates the query instance, registering any necessary filters with the world.
    /// </summary>
    /// <param name="world">The world to create the query against.</param>
    /// <returns>The created query instance.</returns>
    public abstract object CreateQuery(IWorld world);

    /// <summary>
    /// Disposes the query, unregistering any filters.
    /// </summary>
    /// <param name="query">The query instance to dispose.</param>
    public abstract void DisposeQuery(object query);
}
