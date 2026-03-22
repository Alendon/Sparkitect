using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Concrete command buffer accessor implementation.
/// Manages buffer lifecycle: Create allocates EntityId immediately,
/// Modify validates entity state, Playback executes all buffers in FIFO order.
/// </summary>
public class CommandBufferAccessor : ICommandBufferAccessor
{
    private readonly IWorld _world;
    private readonly List<IPlaybackBuffer> _buffers = new();

    public CommandBufferAccessor(IWorld world)
    {
        _world = world;
    }

    /// <inheritdoc/>
    public ICommandBuffer<TKey> Create<TKey>(StorageHandle storageHandle) where TKey : unmanaged
    {
        var entityId = _world.AllocateEntityId();
        var buffer = new CommandBuffer<TKey>(entityId, storageHandle, isCreate: true);
        _buffers.Add(buffer);
        return buffer;
    }

    /// <inheritdoc/>
    public ICommandBuffer<TKey> Create<TKey>(IReadOnlyList<ICapabilityRequirement> filter) where TKey : unmanaged
    {
        var handles = _world.Resolve(filter);
        if (handles.Count == 0)
        {
            throw new InvalidOperationException(
                "No storage matched the provided filter. Cannot create entity without a target storage.");
        }
        return Create<TKey>(handles[0]);
    }

    /// <inheritdoc/>
    public ICommandBuffer<TKey> Modify<TKey>(EntityId entityId) where TKey : unmanaged
    {
        var state = _world.GetEntityState(entityId);
        if (state != EntityState.Bound)
        {
            throw new InvalidOperationException(
                $"Cannot modify entity {entityId}: expected state Bound but found {state}.");
        }

        var storageHandle = _world.GetStorageHandle(entityId);
        var buffer = new CommandBuffer<TKey>(entityId, storageHandle, isCreate: false);
        _buffers.Add(buffer);
        return buffer;
    }

    /// <inheritdoc/>
    public void Playback()
    {
        foreach (var buffer in _buffers)
        {
            buffer.PlaybackCommands(_world);
        }
        _buffers.Clear();
    }
}
