using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// DI parameter interface for systems to create and manage command buffers.
/// One accessor instance is shared per world, resolved via EcsResolutionProvider.
/// All methods are non-generic -- TKey is resolved at runtime via IStorage.KeyType.
/// </summary>
public interface ICommandBufferAccessor
{
    /// <summary>
    /// Creates a new entity buffer targeting the specified storage.
    /// Immediately allocates an EntityId available via the returned buffer.
    /// </summary>
    ICommandBuffer Create(StorageHandle storageHandle);

    /// <summary>
    /// Creates a new entity buffer targeting a storage resolved from the filter.
    /// Uses World.Resolve to find matching storage, takes the first match.
    /// </summary>
    ICommandBuffer Create(IReadOnlyList<ICapabilityRequirement> filter);

    /// <summary>
    /// Creates a modify buffer for an existing entity.
    /// Validates the entity is in Bound state (fail-fast).
    /// </summary>
    /// <exception cref="InvalidOperationException">The entity is not in Bound state.</exception>
    ICommandBuffer Modify(EntityId entityId);

    /// <summary>
    /// Plays back all recorded buffers in FIFO order, then clears.
    /// </summary>
    void Playback();
}
