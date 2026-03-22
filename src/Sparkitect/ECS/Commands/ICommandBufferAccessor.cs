using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// DI parameter interface for systems to create and manage command buffers.
/// One accessor instance is shared per world (D-07), resolved via EcsResolutionProvider.
/// </summary>
public interface ICommandBufferAccessor
{
    /// <summary>
    /// Creates a new entity buffer targeting the specified storage.
    /// Immediately allocates an EntityId (D-14) available via the returned buffer.
    /// </summary>
    /// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
    /// <param name="storageHandle">The storage to create the entity in.</param>
    /// <returns>A buffer for recording mutations on the new entity.</returns>
    ICommandBuffer<TKey> Create<TKey>(StorageHandle storageHandle) where TKey : unmanaged;

    /// <summary>
    /// Creates a new entity buffer targeting a storage resolved from the filter.
    /// Uses World.Resolve to find matching storage, takes the first match.
    /// </summary>
    /// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
    /// <param name="filter">Capability requirements to resolve the target storage.</param>
    /// <returns>A buffer for recording mutations on the new entity.</returns>
    ICommandBuffer<TKey> Create<TKey>(IReadOnlyList<ICapabilityRequirement> filter) where TKey : unmanaged;

    /// <summary>
    /// Creates a modify buffer for an existing entity.
    /// Validates the entity is in Bound state (fail-fast per Pitfall 6).
    /// </summary>
    /// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
    /// <param name="entityId">The existing entity to modify.</param>
    /// <returns>A buffer for recording mutations on the existing entity.</returns>
    /// <exception cref="InvalidOperationException">The entity is not in Bound state.</exception>
    ICommandBuffer<TKey> Modify<TKey>(EntityId entityId) where TKey : unmanaged;

    /// <summary>
    /// Plays back all recorded buffers in FIFO order (D-23), then clears (D-24).
    /// </summary>
    void Playback();
}
