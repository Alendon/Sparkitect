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
    /// Creates a new World instance.
    /// </summary>
    /// <returns>A fresh <see cref="IWorld"/> implementation.</returns>
    static IWorld Create() => new World();
}
