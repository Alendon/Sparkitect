using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Command that destroys an entity at playback time.
/// Unassigns the entity identity, removes it from storage, and reclaims the EntityId.
/// </summary>
/// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
public class DestroyEntityCommand<TKey> : ICommand where TKey : unmanaged
{
    /// <inheritdoc/>
    public void Execute(IWorld world, StorageHandle storageHandle, EntityId entityId)
    {
        var accessor = world.GetStorage(storageHandle);
        var identity = accessor.As<IEntityIdentity<TKey>>()!;
        identity.TryResolve(entityId, out var slot);
        identity.Unassign(entityId);
        var mutation = accessor.As<IEntityMutation<TKey>>()!;
        mutation.RemoveEntity(slot);
        world.ReclaimEntityId(entityId);
    }
}
