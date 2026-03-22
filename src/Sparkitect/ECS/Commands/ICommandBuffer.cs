namespace Sparkitect.ECS.Commands;

/// <summary>
/// Buffer interface for recording deferred mutations against a single entity.
/// Each buffer targets exactly one entity (new or existing) per D-02.
/// </summary>
/// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
public interface ICommandBuffer<TKey> where TKey : unmanaged
{
    /// <summary>
    /// The entity this buffer targets. Available immediately after creation (D-15).
    /// </summary>
    EntityId EntityId { get; }

    /// <summary>
    /// The storage handle this buffer targets for playback.
    /// </summary>
    StorageHandle StorageHandle { get; }

    /// <summary>
    /// The list of recorded commands to be executed at playback.
    /// </summary>
    List<ICommand> Commands { get; }
}
