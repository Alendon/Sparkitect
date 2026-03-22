using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Concrete command buffer implementation targeting a single entity.
/// Implements both the generic <see cref="ICommandBuffer{TKey}"/> for system-facing API
/// and the non-generic <see cref="IPlaybackBuffer"/> for type-erased storage in the accessor.
/// </summary>
/// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
public class CommandBuffer<TKey> : ICommandBuffer<TKey>, IPlaybackBuffer where TKey : unmanaged
{
    /// <inheritdoc/>
    public EntityId EntityId { get; }

    /// <inheritdoc/>
    public StorageHandle StorageHandle { get; }

    /// <inheritdoc/>
    public List<ICommand> Commands { get; } = new();

    /// <inheritdoc/>
    public bool IsCreate { get; }

    internal CommandBuffer(EntityId entityId, StorageHandle storageHandle, bool isCreate)
    {
        EntityId = entityId;
        StorageHandle = storageHandle;
        IsCreate = isCreate;
    }

    /// <inheritdoc/>
    public void PlaybackCommands(IWorld world)
    {
        if (IsCreate)
        {
            var accessor = world.GetStorage(StorageHandle);
            var storage = accessor.AsStorage<TKey>()!;
            var slot = storage.AllocateEntity();
            var identity = accessor.As<IEntityIdentity<TKey>>()!;
            identity.Assign(EntityId, slot);
        }

        foreach (var command in Commands)
        {
            command.Execute(world, StorageHandle, EntityId);
        }
    }
}
