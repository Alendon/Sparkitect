using JetBrains.Annotations;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Non-generic command marker interface for type-erased storage in command lists.
/// Commands encode deferred mutations that execute at playback time.
/// TKey is provided at method level by the buffer during playback.
/// </summary>
[PublicAPI]
public interface ICommand
{
    /// <summary>
    /// Executes the recorded mutation against the world using the pre-resolved slot.
    /// </summary>
    /// <typeparam name="TKey">The unmanaged storage key type, provided by the buffer.</typeparam>
    /// <param name="world">The world to apply the mutation to.</param>
    /// <param name="storageHandle">The storage handle targeting the entity's storage.</param>
    /// <param name="resolvedSlot">The pre-resolved storage slot for this entity.</param>
    void Execute<TKey>(IWorld world, StorageHandle storageHandle, TKey resolvedSlot)
        where TKey : unmanaged;
}
