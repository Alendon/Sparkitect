namespace Sparkitect.ECS.Commands;

/// <summary>
/// Non-generic command marker interface for type-erased storage in command lists.
/// Commands encode deferred mutations that execute at playback time.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the recorded mutation against the world.
    /// </summary>
    /// <param name="world">The world to apply the mutation to.</param>
    /// <param name="storageHandle">The storage handle targeting the entity's storage.</param>
    /// <param name="entityId">The entity this command targets.</param>
    void Execute(IWorld world, StorageHandle storageHandle, EntityId entityId);
}
