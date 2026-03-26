using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Command that destroys an entity at playback time.
/// Receives the pre-resolved slot from the buffer -- unassign, remove, and reclaim.
/// </summary>
public class DestroyEntityCommand : ICommand
{
    /// <inheritdoc/>
    public void Execute<TKey>(IWorld world, StorageHandle storageHandle, TKey resolvedSlot)
        where TKey : unmanaged
    {
        var accessor = world.GetStorage(storageHandle);
        var identity = accessor.As<IEntityIdentity<TKey>>()!;
        var entityId = identity.GetEntityId(resolvedSlot);
        identity.Unassign(entityId);
        var mutation = accessor.As<IEntityMutation<TKey>>()!;
        mutation.RemoveEntity(resolvedSlot);
        world.ReclaimEntityId(entityId);
    }
}
