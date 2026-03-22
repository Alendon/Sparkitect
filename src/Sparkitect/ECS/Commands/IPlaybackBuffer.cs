namespace Sparkitect.ECS.Commands;

/// <summary>
/// Non-generic interface for type-erased buffer storage in the accessor's buffer list.
/// Enables heterogeneous collection of buffers with different TKey types.
/// </summary>
internal interface IPlaybackBuffer
{
    /// <summary>
    /// The entity this buffer targets.
    /// </summary>
    EntityId EntityId { get; }

    /// <summary>
    /// The storage handle this buffer targets.
    /// </summary>
    StorageHandle StorageHandle { get; }

    /// <summary>
    /// Whether this buffer represents entity creation (vs modification).
    /// </summary>
    bool IsCreate { get; }

    /// <summary>
    /// Executes the create preamble (if applicable) and all recorded commands against the world.
    /// </summary>
    /// <param name="world">The world to play back against.</param>
    void PlaybackCommands(IWorld world);
}
