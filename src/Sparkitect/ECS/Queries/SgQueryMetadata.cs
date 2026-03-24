using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Bridge metadata for SG-generated query classes. Takes a factory function and
/// component ID lists from the generated query's static members. Used by hand-written
/// <c>IResolutionMetadataEntrypoint</c> implementations until Phase 43 auto-generates
/// resolution metadata.
/// </summary>
public class SgQueryMetadata<TQuery> : QueryParameterMetadata
    where TQuery : IDisposable
{
    private readonly IReadOnlyList<Identification> _readComponentIds;
    private readonly IReadOnlyList<Identification> _writeComponentIds;
    private readonly Func<IWorld, TQuery> _factory;

    /// <summary>
    /// Creates bridge metadata for an SG-generated query type.
    /// </summary>
    /// <param name="readComponentIds">The query's static ReadComponentIds list.</param>
    /// <param name="writeComponentIds">The query's static WriteComponentIds list.</param>
    /// <param name="factory">Factory that creates the query via its IWorld constructor.</param>
    public SgQueryMetadata(
        IReadOnlyList<Identification> readComponentIds,
        IReadOnlyList<Identification> writeComponentIds,
        Func<IWorld, TQuery> factory)
    {
        _readComponentIds = readComponentIds;
        _writeComponentIds = writeComponentIds;
        _factory = factory;
    }

    /// <inheritdoc/>
    public override object CreateQuery(IWorld world) => _factory(world);

    /// <inheritdoc/>
    public override void DisposeQuery(object query) => ((IDisposable)query).Dispose();
}
