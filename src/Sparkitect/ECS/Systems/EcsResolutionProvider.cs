using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Resolution provider for ECS query parameters and command buffer accessors.
/// Reads <see cref="QueryParameterMetadata"/> entries from the metadata list, creates
/// query instances via the metadata's CreateQuery, and tracks them for lifecycle cleanup.
/// Reads <see cref="CommandBufferAccessorMetadata"/> entries and returns the shared
/// accessor set via <see cref="SetCommandBufferAccessor"/>.
/// </summary>
internal class EcsResolutionProvider : IResolutionProvider
{
    private readonly IWorld _world;
    private readonly List<QueryParameterMetadata> _trackedMetadata = new();
    private readonly List<object> _trackedQueries = new();
    private ICommandBufferAccessor? _commandBufferAccessor;

    internal EcsResolutionProvider(IWorld world)
    {
        _world = world;
    }

    /// <summary>
    /// Sets the shared command buffer accessor for this provider.
    /// Called by SystemManager.BuildWorldCache after creating the accessor.
    /// </summary>
    internal void SetCommandBufferAccessor(ICommandBufferAccessor accessor)
    {
        _commandBufferAccessor = accessor;
    }

    /// <inheritdoc/>
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        foreach (var entry in metadataEntries)
        {
            if (entry is QueryParameterMetadata queryMeta)
            {
                var query = queryMeta.CreateQuery(_world);
                _trackedMetadata.Add(queryMeta);
                _trackedQueries.Add(query);
                service = query;
                return true;
            }

            if (entry is CommandBufferAccessorMetadata)
            {
                service = _commandBufferAccessor
                    ?? throw new InvalidOperationException(
                        "CommandBufferAccessor not set on EcsResolutionProvider.");
                return true;
            }
        }

        service = null;
        return false;
    }

    /// <summary>
    /// Disposes all tracked queries by calling DisposeQuery on their metadata.
    /// Called by SystemManager on NotifyDispose/NotifyRebuild.
    /// </summary>
    internal void CleanupQueries()
    {
        for (int i = 0; i < _trackedMetadata.Count; i++)
        {
            _trackedMetadata[i].DisposeQuery(_trackedQueries[i]);
        }
        _trackedMetadata.Clear();
        _trackedQueries.Clear();
    }
}
