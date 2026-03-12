using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;

namespace Sparkitect.ECS;

/// <summary>
/// Public contract for the ECS world coordinator.
/// Manages storage lifecycle, capability-based discovery, and reactive filters.
/// </summary>
public interface IWorld : IDisposable
{
    /// <summary>
    /// Adds a storage to the world with its capability registrations.
    /// Returns a handle that can be used to access or remove the storage.
    /// </summary>
    /// <param name="storage">The storage instance to add.</param>
    /// <param name="capabilities">Capability registrations declaring what the storage provides.</param>
    /// <returns>A generational handle identifying the storage.</returns>
    StorageHandle AddStorage(IStorage storage, IReadOnlyList<CapabilityRegistration> capabilities);

    /// <summary>
    /// Removes a storage from the world, disposing it and invalidating its handle.
    /// </summary>
    /// <param name="handle">The handle of the storage to remove.</param>
    /// <exception cref="InvalidOperationException">The handle is stale or invalid.</exception>
    void RemoveStorage(StorageHandle handle);

    /// <summary>
    /// Returns a stack-only accessor for the storage identified by the handle.
    /// </summary>
    /// <param name="handle">The handle of the storage to access.</param>
    /// <returns>A <see cref="StorageAccessor"/> wrapping the storage.</returns>
    /// <exception cref="InvalidOperationException">The handle is stale or invalid.</exception>
    StorageAccessor GetStorage(StorageHandle handle);

    /// <summary>
    /// Resolves all storages whose capabilities satisfy every requirement in the filter.
    /// </summary>
    /// <param name="filter">Requirements that must all be matched by a storage's capabilities.</param>
    /// <returns>Handles of all matching storages.</returns>
    IReadOnlyList<StorageHandle> Resolve(IReadOnlyList<ICapabilityRequirement> filter);

    /// <summary>
    /// Registers a long-lived filter that fires a callback whenever the set of matching storages changes.
    /// The callback fires immediately with the current matching set.
    /// </summary>
    /// <param name="filter">Requirements for the filter.</param>
    /// <param name="callback">Callback invoked with the current set of matching handles.</param>
    /// <returns>A handle that can be used to unregister the filter.</returns>
    FilterHandle RegisterFilter(
        IReadOnlyList<ICapabilityRequirement> filter,
        Action<IReadOnlyList<StorageHandle>> callback);

    /// <summary>
    /// Unregisters a previously registered filter. The callback will no longer fire on topology changes.
    /// </summary>
    /// <param name="handle">The handle returned by <see cref="RegisterFilter"/>.</param>
    void UnregisterFilter(FilterHandle handle);

    /// <summary>
    /// Allocates a new entity ID with an incremented generation. The entity starts in <see cref="EntityState.Empty"/>.
    /// </summary>
    /// <returns>A valid <see cref="EntityId"/> with generation >= 1.</returns>
    EntityId AllocateEntityId();

    /// <summary>
    /// Hard reclaim: unconditionally reclaims the entity ID, invalidating it and recycling the slot.
    /// </summary>
    /// <param name="id">The entity to reclaim.</param>
    /// <returns>True if the entity was valid and reclaimed; false if already dead.</returns>
    bool ReclaimEntityId(EntityId id);

    /// <summary>
    /// Soft reclaim: reclaims only if the entity's current storage binding matches the given handle.
    /// Prevents accidental reclaim by a system that no longer owns the entity.
    /// </summary>
    /// <param name="id">The entity to reclaim.</param>
    /// <param name="storageHandle">The expected storage binding.</param>
    /// <returns>True if the entity was valid, binding matched, and was reclaimed; false otherwise.</returns>
    bool TryReclaimEntityId(EntityId id, StorageHandle storageHandle);

    /// <summary>
    /// Returns whether the entity ID is currently valid (allocated, not reclaimed, correct generation).
    /// </summary>
    /// <param name="id">The entity ID to validate.</param>
    /// <returns>True if the entity is alive.</returns>
    bool IsValid(EntityId id);

    /// <summary>
    /// Transitions an entity from <see cref="EntityState.Empty"/> to <see cref="EntityState.Bound"/>,
    /// associating it with a storage.
    /// </summary>
    /// <param name="id">The entity to bind.</param>
    /// <param name="storageHandle">The storage to bind the entity to.</param>
    /// <exception cref="InvalidOperationException">The entity is not in Empty state.</exception>
    void BindEntity(EntityId id, StorageHandle storageHandle);

    /// <summary>
    /// Returns the current lifecycle state of the entity.
    /// </summary>
    /// <param name="id">The entity ID to query.</param>
    /// <returns>The entity's current state.</returns>
    EntityState GetEntityState(EntityId id);

    /// <summary>
    /// Returns the storage handle bound to the entity.
    /// </summary>
    /// <param name="id">The entity ID to query.</param>
    /// <returns>The storage handle the entity is bound to.</returns>
    /// <exception cref="InvalidOperationException">The entity is not in Bound state.</exception>
    StorageHandle GetStorageHandle(EntityId id);

    /// <summary>
    /// Convenience method: resolves the entity's storage binding and returns an accessor.
    /// Equivalent to <c>GetStorage(GetStorageHandle(id))</c>.
    /// </summary>
    /// <param name="id">The entity ID to query.</param>
    /// <returns>A <see cref="StorageAccessor"/> for the entity's bound storage.</returns>
    StorageAccessor GetStorage(EntityId id);

    /// <summary>
    /// Creates a new World instance.
    /// </summary>
    /// <returns>A fresh <see cref="IWorld"/> implementation.</returns>
    static IWorld Create() => new World();
}
