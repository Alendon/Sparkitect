using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Resolution provider for ECS query parameters, command buffer accessors, and frame timing.
/// Resolves FrameTimingHolder and ICommandBufferAccessor by direct type check.
/// Reads <see cref="QueryParameterMetadata"/> entries from the metadata list for query parameters.
/// </summary>
internal class EcsResolutionProvider : IResolutionProvider
{
    private readonly IWorld _world;
    private readonly List<QueryParameterMetadata> _trackedMetadata = new();
    private readonly List<object> _trackedQueries = new();
    private ICommandBufferAccessor? _commandBufferAccessor;
    private FrameTimingHolder? _frameTimingHolder;

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

    /// <summary>
    /// Sets the shared frame timing holder for this provider.
    /// Called by SystemManager.BuildWorldCache after creating the holder.
    /// </summary>
    internal void SetFrameTimingHolder(FrameTimingHolder holder)
    {
        _frameTimingHolder = holder;
    }

    /// <inheritdoc/>
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        // Direct type checks -- no metadata entries needed (D-08, D-09, D-10)
        if (serviceType == typeof(FrameTimingHolder))
        {
            service = _frameTimingHolder
                ?? throw new InvalidOperationException(
                    "FrameTimingHolder not set on EcsResolutionProvider.");
            return true;
        }

        if (serviceType == typeof(ICommandBufferAccessor))
        {
            service = _commandBufferAccessor
                ?? throw new InvalidOperationException(
                    "CommandBufferAccessor not set on EcsResolutionProvider.");
            return true;
        }

        // Query parameter resolution via metadata entries
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
