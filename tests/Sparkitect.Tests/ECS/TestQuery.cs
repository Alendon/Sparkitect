using System.Collections;
using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

/// <summary>
/// Minimal query class for engine tests. Replaces the deleted hand-written ComponentQuery
/// for test infrastructure purposes. Production code uses SG-generated query types.
/// </summary>
internal class TestQuery : IEnumerable<EntityAccessor>, IDisposable
{
    private readonly IWorld _world;
    private readonly IReadOnlyList<Identification> _componentIds;
    private readonly List<StorageHandle> _matchedStorages;
    private readonly FilterHandle _filterHandle;

    public TestQuery(IWorld world, IReadOnlyList<Identification> componentIds)
    {
        _world = world;
        _componentIds = componentIds;
        _matchedStorages = new();

        ICapabilityRequirement[] filter = [new ComponentSetRequirement(componentIds)];
        _filterHandle = world.RegisterFilter(filter, storages =>
        {
            _matchedStorages.Clear();
            _matchedStorages.AddRange(storages);
        });
    }

    public TestQueryEnumerator GetEnumerator()
        => new(_world, _componentIds, _matchedStorages);

    IEnumerator<EntityAccessor> IEnumerable<EntityAccessor>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose() => _world.UnregisterFilter(_filterHandle);

    public unsafe struct TestQueryEnumerator : IEnumerator<EntityAccessor>
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

        internal TestQueryEnumerator(
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
                if (_hasChunk && _entityIndex < _chunkLength)
                {
                    _current = new EntityAccessor(_componentPointers!, _entityIndex);
                    _entityIndex++;
                    return true;
                }

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

                _storageIndex++;
                if (_storageIndex >= _matchedStorages.Count)
                    return false;

                var handle = _matchedStorages[_storageIndex];
                _currentIteration = _world.GetStorage(handle).As<IChunkedIteration>();
                _chunkHandle = default;
                _hasChunk = false;

                if (_currentIteration is null)
                    continue;

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
/// Test helper metadata that creates a <see cref="TestQuery"/> for engine tests.
/// Replaces the deleted ComponentQueryMetadata for test infrastructure purposes.
/// </summary>
internal class TestQueryMetadata : QueryParameterMetadata
{
    private readonly IReadOnlyList<Identification> _componentIds;

    public TestQueryMetadata(IReadOnlyList<Identification> componentIds)
    {
        _componentIds = componentIds;
    }

    public override object CreateQuery(IWorld world) => new TestQuery(world, _componentIds);

    public override void DisposeQuery(object query) => ((TestQuery)query).Dispose();
}
