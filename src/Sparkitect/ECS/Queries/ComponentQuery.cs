using System.Collections;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Stock query wrapping chunk-based iteration into a flat foreach over entities.
/// Storage and chunk boundaries are hidden from the consumer -- iteration yields
/// one <see cref="EntityAccessor"/> per entity across all matching storages.
/// </summary>
[AllowConcreteResolution]
public class ComponentQuery : IEnumerable<EntityAccessor>, IDisposable
{
    private readonly IWorld _world;
    private readonly IReadOnlyList<Identification> _componentIds;
    private readonly List<StorageHandle> _matchedStorages;
    private readonly FilterHandle _filterHandle;

    protected internal ComponentQuery(
        IWorld world,
        IReadOnlyList<Identification> componentIds,
        List<StorageHandle> matchedStorages,
        FilterHandle filterHandle)
    {
        _world = world;
        _componentIds = componentIds;
        _matchedStorages = matchedStorages;
        _filterHandle = filterHandle;
    }

    /// <summary>
    /// Returns an enumerator that iterates all entities across all matching storages.
    /// </summary>
    public ComponentQueryEnumerator GetEnumerator()
    {
        return new ComponentQueryEnumerator(_world, _componentIds, _matchedStorages);
    }

    IEnumerator<EntityAccessor> IEnumerable<EntityAccessor>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Unregisters the filter from the world.
    /// </summary>
    public void Dispose()
    {
        _world.UnregisterFilter(_filterHandle);
    }

    /// <summary>
    /// Enumerator walking matched storages, chunks within each storage, and entities within each chunk.
    /// Yields one <see cref="EntityAccessor"/> per entity with component pointers for the current chunk.
    /// </summary>
    public unsafe struct ComponentQueryEnumerator : IEnumerator<EntityAccessor>
    {
        private readonly IWorld _world;
        private readonly IReadOnlyList<Identification> _componentIds;
        private readonly List<StorageHandle> _matchedStorages;

        private int _storageIndex;
        private IChunkedIteration? _currentIteration;
        private ChunkHandle _chunkHandle;
        private int _chunkLength;
        private int _entityIndex;
        private Dictionary<Identification, nint>? _componentPointers;
        private bool _hasChunk;
        private EntityAccessor _current;

        internal ComponentQueryEnumerator(
            IWorld world,
            IReadOnlyList<Identification> componentIds,
            List<StorageHandle> matchedStorages)
        {
            _world = world;
            _componentIds = componentIds;
            _matchedStorages = matchedStorages;
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _componentPointers = null;
            _hasChunk = false;
            _current = default;
        }

        public EntityAccessor Current => _current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (true)
            {
                // Try advancing within current chunk
                if (_hasChunk && _entityIndex < _chunkLength)
                {
                    _current = new EntityAccessor(_componentPointers!, _entityIndex);
                    _entityIndex++;
                    return true;
                }

                // Try next chunk in current storage
                if (_currentIteration is not null)
                {
                    if (_currentIteration.GetNextChunk(ref _chunkHandle, out _chunkLength))
                    {
                        _hasChunk = true;
                        _entityIndex = 0;
                        BuildComponentPointers();
                        continue;
                    }
                }

                // Move to next storage
                _storageIndex++;
                if (_storageIndex >= _matchedStorages.Count)
                    return false;

                var handle = _matchedStorages[_storageIndex];
                _currentIteration = _world.GetStorage(handle).As<IChunkedIteration>();
                _chunkHandle = default; // Fresh per storage (pitfall 3)
                _hasChunk = false;

                if (_currentIteration is null)
                    continue;

                // Immediately try to get first chunk of new storage
                if (_currentIteration.GetNextChunk(ref _chunkHandle, out _chunkLength))
                {
                    _hasChunk = true;
                    _entityIndex = 0;
                    BuildComponentPointers();
                    continue;
                }
            }
        }

        private void BuildComponentPointers()
        {
            _componentPointers = new Dictionary<Identification, nint>(_componentIds.Count);
            foreach (var componentId in _componentIds)
            {
                var ptr = _currentIteration!.GetChunkComponentData(ref _chunkHandle, componentId);
                _componentPointers[componentId] = (nint)ptr;
            }
        }

        public void Reset()
        {
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _componentPointers = null;
            _hasChunk = false;
            _current = default;
        }

        public void Dispose() { }
    }
}

/// <summary>
/// Generic keyed query wrapping chunk-based iteration with entity key access.
/// Yields <see cref="KeyedEntityAccessor{TKey}"/> per entity, enabling systems to
/// access entity keys (e.g., EntityId) during iteration for command buffer operations.
/// </summary>
[AllowConcreteResolution]
public class ComponentQuery<TKey> : IEnumerable<KeyedEntityAccessor<TKey>>, IDisposable
    where TKey : unmanaged
{
    private readonly IWorld _world;
    private readonly IReadOnlyList<Identification> _componentIds;
    private readonly List<StorageHandle> _matchedStorages;
    private readonly FilterHandle _filterHandle;

    protected internal ComponentQuery(
        IWorld world,
        IReadOnlyList<Identification> componentIds,
        List<StorageHandle> matchedStorages,
        FilterHandle filterHandle)
    {
        _world = world;
        _componentIds = componentIds;
        _matchedStorages = matchedStorages;
        _filterHandle = filterHandle;
    }

    /// <summary>
    /// Returns an enumerator that iterates all entities with their keys across all matching storages.
    /// </summary>
    public ComponentQueryKeyedEnumerator<TKey> GetEnumerator()
        => new(_world, _componentIds, _matchedStorages);

    IEnumerator<KeyedEntityAccessor<TKey>> IEnumerable<KeyedEntityAccessor<TKey>>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Unregisters the filter from the world.
    /// </summary>
    public void Dispose() => _world.UnregisterFilter(_filterHandle);

    /// <summary>
    /// Enumerator walking matched storages, chunks within each storage, and entities within each chunk.
    /// Yields one <see cref="KeyedEntityAccessor{TKey}"/> per entity with key from
    /// <see cref="IChunkedIteration{TKey}.GetKey"/>.
    /// </summary>
    public unsafe struct ComponentQueryKeyedEnumerator<TEnumKey> : IEnumerator<KeyedEntityAccessor<TEnumKey>>
        where TEnumKey : unmanaged
    {
        private readonly IWorld _world;
        private readonly IReadOnlyList<Identification> _componentIds;
        private readonly List<StorageHandle> _matchedStorages;

        private int _storageIndex;
        private IChunkedIteration<TEnumKey>? _currentIteration;
        private ChunkHandle _chunkHandle;
        private int _chunkLength;
        private int _entityIndex;
        private Dictionary<Identification, nint>? _componentPointers;
        private bool _hasChunk;
        private KeyedEntityAccessor<TEnumKey> _current;

        internal ComponentQueryKeyedEnumerator(
            IWorld world,
            IReadOnlyList<Identification> componentIds,
            List<StorageHandle> matchedStorages)
        {
            _world = world;
            _componentIds = componentIds;
            _matchedStorages = matchedStorages;
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _componentPointers = null;
            _hasChunk = false;
            _current = default;
        }

        public KeyedEntityAccessor<TEnumKey> Current => _current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (true)
            {
                // Try advancing within current chunk
                if (_hasChunk && _entityIndex < _chunkLength)
                {
                    var key = _currentIteration!.GetKey(ref _chunkHandle, _entityIndex);
                    _current = new KeyedEntityAccessor<TEnumKey>(
                        new EntityAccessor(_componentPointers!, _entityIndex), key);
                    _entityIndex++;
                    return true;
                }

                // Try next chunk in current storage
                if (_currentIteration is not null)
                {
                    if (_currentIteration.GetNextChunk(ref _chunkHandle, out _chunkLength))
                    {
                        _hasChunk = true;
                        _entityIndex = 0;
                        BuildComponentPointers();
                        continue;
                    }
                }

                // Move to next storage
                _storageIndex++;
                if (_storageIndex >= _matchedStorages.Count)
                    return false;

                var handle = _matchedStorages[_storageIndex];
                _currentIteration = _world.GetStorage(handle).As<IChunkedIteration<TEnumKey>>();
                _chunkHandle = default;
                _hasChunk = false;

                if (_currentIteration is null)
                    continue;

                // Immediately try to get first chunk of new storage
                if (_currentIteration.GetNextChunk(ref _chunkHandle, out _chunkLength))
                {
                    _hasChunk = true;
                    _entityIndex = 0;
                    BuildComponentPointers();
                    continue;
                }
            }
        }

        private void BuildComponentPointers()
        {
            _componentPointers = new Dictionary<Identification, nint>(_componentIds.Count);
            foreach (var componentId in _componentIds)
            {
                var ptr = _currentIteration!.GetChunkComponentData(ref _chunkHandle, componentId);
                _componentPointers[componentId] = (nint)ptr;
            }
        }

        public void Reset()
        {
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _componentPointers = null;
            _hasChunk = false;
            _current = default;
        }

        public void Dispose() { }
    }
}
