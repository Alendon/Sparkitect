using Serilog;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;

namespace Sparkitect.ECS;

/// <summary>
/// Central ECS coordinator managing storage lifecycle and capability discovery.
/// Uses generational slot management with parallel arrays for efficient storage access.
/// </summary>
internal class World : IWorld
{
    private const int InitialCapacity = 4;

    // Parallel arrays indexed by storage slot
    private IStorage?[] _storages;
    private List<CapabilityRegistration>?[] _capabilities;
    private uint[] _generations;

    // Free list for slot recycling
    private readonly Stack<uint> _freeSlots = new();
    private int _highWaterMark;
    private bool _disposed;

    // Filter registry
    private readonly List<FilterEntry> _filters = new();

    /// <summary>
    /// Creates a new World with default initial capacity.
    /// </summary>
    public World()
    {
        _storages = new IStorage?[InitialCapacity];
        _capabilities = new List<CapabilityRegistration>?[InitialCapacity];
        _generations = new uint[InitialCapacity];
    }

    /// <summary>
    /// Adds a storage to the world with its capability registrations.
    /// Returns a handle that can be used to access or remove the storage.
    /// </summary>
    /// <param name="storage">The storage instance to add.</param>
    /// <param name="capabilities">Capability registrations declaring what the storage provides.</param>
    /// <returns>A generational handle identifying the storage.</returns>
    public StorageHandle AddStorage(IStorage storage, IReadOnlyList<CapabilityRegistration> capabilities)
    {
        ThrowIfDisposed();

        uint slotIndex;
        if (_freeSlots.Count > 0)
        {
            slotIndex = _freeSlots.Pop();
        }
        else
        {
            if (_highWaterMark >= _storages.Length)
            {
                GrowArrays();
            }
            slotIndex = (uint)_highWaterMark;
            _highWaterMark++;
        }

        _storages[slotIndex] = storage;
        _capabilities[slotIndex] = new List<CapabilityRegistration>(capabilities);

        var handle = new StorageHandle(slotIndex, _generations[slotIndex]);

        NotifyFilters();

        return handle;
    }

    /// <summary>
    /// Removes a storage from the world, disposing it and invalidating its handle.
    /// </summary>
    /// <param name="handle">The handle of the storage to remove.</param>
    /// <exception cref="InvalidOperationException">The handle is stale or invalid.</exception>
    public void RemoveStorage(StorageHandle handle)
    {
        ThrowIfDisposed();
        ValidateHandle(handle);

        var storage = _storages[handle.Index]!;
        _storages[handle.Index] = null;
        _capabilities[handle.Index] = null;
        _generations[handle.Index]++;
        _freeSlots.Push(handle.Index);

        try
        {
            storage.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception disposing storage at slot {Index}", handle.Index);
        }

        NotifyFilters();
    }

    /// <summary>
    /// Returns a stack-only accessor for the storage identified by the handle.
    /// </summary>
    /// <param name="handle">The handle of the storage to access.</param>
    /// <returns>A <see cref="StorageAccessor"/> wrapping the storage.</returns>
    /// <exception cref="InvalidOperationException">The handle is stale or invalid.</exception>
    public StorageAccessor GetStorage(StorageHandle handle)
    {
        ThrowIfDisposed();
        ValidateHandle(handle);

        return new StorageAccessor(_storages[handle.Index]!, handle);
    }

    /// <summary>
    /// Resolves all storages whose capabilities satisfy every requirement in the filter.
    /// </summary>
    /// <param name="filter">Requirements that must all be matched by a storage's capabilities.</param>
    /// <returns>Handles of all matching storages.</returns>
    public IReadOnlyList<StorageHandle> Resolve(IReadOnlyList<ICapabilityRequirement> filter)
    {
        ThrowIfDisposed();
        return EvaluateFilter(filter);
    }

    /// <summary>
    /// Registers a long-lived filter that fires a callback whenever the set of matching storages changes.
    /// The callback fires immediately with the current matching set.
    /// </summary>
    /// <param name="filter">Requirements for the filter.</param>
    /// <param name="callback">Callback invoked with the current set of matching handles.</param>
    /// <returns>A handle that can be used to unregister the filter.</returns>
    public FilterHandle RegisterFilter(
        IReadOnlyList<ICapabilityRequirement> filter,
        Action<IReadOnlyList<StorageHandle>> callback)
    {
        ThrowIfDisposed();

        var entry = new FilterEntry(filter, callback);
        _filters.Add(entry);
        var filterHandle = new FilterHandle(_filters.Count - 1);

        // Fire immediately with current matches
        var matches = EvaluateFilter(filter);
        callback(matches);

        return filterHandle;
    }

    /// <summary>
    /// Unregisters a previously registered filter. The callback will no longer fire on topology changes.
    /// </summary>
    /// <param name="handle">The handle returned by <see cref="RegisterFilter"/>.</param>
    public void UnregisterFilter(FilterHandle handle)
    {
        ThrowIfDisposed();

        if (handle.Index >= 0 && handle.Index < _filters.Count)
        {
            _filters[handle.Index].Active = false;
        }
    }

    /// <summary>
    /// Disposes all active storages with per-item exception handling.
    /// After disposal, all handles are stale and double-dispose is safe.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        for (int i = 0; i < _highWaterMark; i++)
        {
            var storage = _storages[i];
            if (storage is null) continue;

            _storages[i] = null;
            _capabilities[i] = null;
            _generations[i]++;

            try
            {
                storage.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception disposing storage at slot {Index}", i);
            }
        }
    }

    private void ValidateHandle(StorageHandle handle)
    {
        if (handle.Index >= (uint)_highWaterMark)
        {
            throw new InvalidOperationException(
                $"Storage handle {handle} references slot {handle.Index} which is out of range (high water mark: {_highWaterMark}).");
        }

        if (_generations[handle.Index] != handle.Generation)
        {
            throw new InvalidOperationException(
                $"Storage handle {handle} is stale. Expected generation {_generations[handle.Index]}, got {handle.Generation}.");
        }

        if (_storages[handle.Index] is null)
        {
            throw new InvalidOperationException(
                $"Storage handle {handle} references an empty slot.");
        }
    }

    private List<StorageHandle> EvaluateFilter(IReadOnlyList<ICapabilityRequirement> filter)
    {
        var results = new List<StorageHandle>();

        for (int i = 0; i < _highWaterMark; i++)
        {
            if (_storages[i] is null) continue;
            var caps = _capabilities[i];
            if (caps is null) continue;

            bool allMatch = true;
            for (int r = 0; r < filter.Count; r++)
            {
                if (!MatchesRequirement(filter[r], caps))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                results.Add(new StorageHandle((uint)i, _generations[i]));
            }
        }

        return results;
    }

    private static bool MatchesRequirement(
        ICapabilityRequirement requirement,
        List<CapabilityRegistration> capabilities)
    {
        for (int i = 0; i < capabilities.Count; i++)
        {
            if (capabilities[i].TryMatch(requirement))
                return true;
        }

        return false;
    }

    private void NotifyFilters()
    {
        for (int i = 0; i < _filters.Count; i++)
        {
            var entry = _filters[i];
            if (!entry.Active) continue;

            var matches = EvaluateFilter(entry.Filter);
            entry.Callback(matches);
        }
    }

    private void GrowArrays()
    {
        var newCapacity = _storages.Length * 2;
        Array.Resize(ref _storages, newCapacity);
        Array.Resize(ref _capabilities, newCapacity);
        Array.Resize(ref _generations, newCapacity);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new InvalidOperationException("World has been disposed.");
        }
    }

    private sealed class FilterEntry
    {
        public IReadOnlyList<ICapabilityRequirement> Filter { get; }
        public Action<IReadOnlyList<StorageHandle>> Callback { get; }
        public bool Active { get; set; }

        public FilterEntry(
            IReadOnlyList<ICapabilityRequirement> filter,
            Action<IReadOnlyList<StorageHandle>> callback)
        {
            Filter = filter;
            Callback = callback;
            Active = true;
        }
    }
}
