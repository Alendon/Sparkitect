namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability interface for entity identity tracking within a storage.
/// Maps opaque <see cref="EntityId"/> handles to storage-internal slot keys,
/// enabling external code to reference entities by generational ID.
/// </summary>
/// <typeparam name="TKey">The storage-internal key type (e.g., int for SoA slot index).</typeparam>
public interface IEntityIdentity<TKey> : ICapability
    where TKey : unmanaged
{
    /// <summary>
    /// Assigns a bidirectional mapping between an entity ID and a storage slot key.
    /// </summary>
    /// <param name="id">The entity ID to map.</param>
    /// <param name="slot">The storage slot key to associate.</param>
    void Assign(EntityId id, TKey slot);

    /// <summary>
    /// Attempts to resolve the storage slot key for the given entity ID.
    /// </summary>
    /// <param name="id">The entity ID to look up.</param>
    /// <param name="slot">The resolved slot key, if found.</param>
    /// <returns>True if the entity has a mapping; false otherwise.</returns>
    bool TryResolve(EntityId id, out TKey slot);

    /// <summary>
    /// Removes the mapping for the given entity ID.
    /// </summary>
    /// <param name="id">The entity ID to unmap.</param>
    void Unassign(EntityId id);

    /// <summary>
    /// Returns the entity ID currently mapped to the given storage slot key.
    /// </summary>
    /// <param name="slot">The slot key to look up.</param>
    /// <returns>The mapped entity ID.</returns>
    /// <exception cref="KeyNotFoundException">The key has no entity mapping.</exception>
    EntityId GetEntityId(TKey slot);
}
