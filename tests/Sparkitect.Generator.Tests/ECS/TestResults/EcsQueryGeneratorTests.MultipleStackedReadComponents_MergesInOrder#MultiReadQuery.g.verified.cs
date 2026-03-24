//HintName: MultiReadQuery.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591
#nullable enable

namespace TestMod;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
partial class MultiReadQuery : global::System.IDisposable
{
    private readonly global::Sparkitect.ECS.IWorld _world;
    private readonly global::System.Collections.Generic.List<global::Sparkitect.ECS.StorageHandle> _matchedStorages;
    private readonly global::Sparkitect.ECS.FilterHandle _filterHandle;

    public static global::System.Collections.Generic.IReadOnlyList<global::Sparkitect.Modding.Identification> ReadComponentIds { get; } =
    
    [
        
        global::TestMod.Position.Identification,
        
        global::TestMod.Velocity.Identification,
        
        global::TestMod.Health.Identification,
        
    ];
    

    public static global::System.Collections.Generic.IReadOnlyList<global::Sparkitect.Modding.Identification> WriteComponentIds { get; } =
    
        global::System.Array.Empty<global::Sparkitect.Modding.Identification>();
    

    public static global::System.Collections.Generic.IReadOnlyList<global::Sparkitect.Modding.Identification> ExcludeComponentIds { get; } =
    
        global::System.Array.Empty<global::Sparkitect.Modding.Identification>();
    

    public MultiReadQuery(global::Sparkitect.ECS.IWorld world)
    {
        _world = world;
        global::Sparkitect.ECS.Capabilities.ICapabilityRequirement[] filter =
        
            [new global::Sparkitect.ECS.Queries.ComponentSetRequirement(
        
                new global::Sparkitect.Modding.Identification[]
                {
                    
                    global::TestMod.Position.Identification,
                    
                    global::TestMod.Velocity.Identification,
                    
                    global::TestMod.Health.Identification,
                    
                })];
        _matchedStorages = new();
        _filterHandle = world.RegisterFilter(filter, storages =>
        {
            _matchedStorages.Clear();
            _matchedStorages.AddRange(storages);
        });
    }

    public void Dispose() => _world.UnregisterFilter(_filterHandle);

    public Enumerator GetEnumerator() => new(_world, _matchedStorages);

    public readonly unsafe struct Entity
    {
        
        private readonly nint _positionPtr;
        
        private readonly nint _velocityPtr;
        
        private readonly nint _healthPtr;
        
        private readonly int _index;
        

        internal Entity(
            
            nint positionPtr,
            
            nint velocityPtr,
            
            nint healthPtr,
            
            int index)
        {
            
            _positionPtr = positionPtr;
            
            _velocityPtr = velocityPtr;
            
            _healthPtr = healthPtr;
            
            _index = index;
            
        }

        

        
        public ref readonly global::TestMod.Position GetPosition()
        {
            return ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::TestMod.Position>(
                (void*)(_positionPtr + _index * sizeof(global::TestMod.Position)));
        }
        
        public ref readonly global::TestMod.Velocity GetVelocity()
        {
            return ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::TestMod.Velocity>(
                (void*)(_velocityPtr + _index * sizeof(global::TestMod.Velocity)));
        }
        
        public ref readonly global::TestMod.Health GetHealth()
        {
            return ref global::System.Runtime.CompilerServices.Unsafe.AsRef<global::TestMod.Health>(
                (void*)(_healthPtr + _index * sizeof(global::TestMod.Health)));
        }
        

        
    }

    public unsafe struct Enumerator : global::System.Collections.Generic.IEnumerator<Entity>
    {
        private readonly global::Sparkitect.ECS.IWorld _world;
        private readonly global::System.Collections.Generic.List<global::Sparkitect.ECS.StorageHandle> _matchedStorages;

        private int _storageIndex;
        
        private global::Sparkitect.ECS.Capabilities.IChunkedIteration? _currentIteration;
        
        private global::Sparkitect.ECS.Storage.ChunkHandle _chunkHandle;
        private int _chunkLength;
        private int _entityIndex;
        private bool _hasChunk;
        private Entity _current;

        
        private nint _positionPtr;
        
        private nint _velocityPtr;
        
        private nint _healthPtr;
        

        internal Enumerator(
            global::Sparkitect.ECS.IWorld world,
            global::System.Collections.Generic.List<global::Sparkitect.ECS.StorageHandle> matchedStorages)
        {
            _world = world;
            _matchedStorages = matchedStorages;
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _hasChunk = false;
            _current = default;
            
            _positionPtr = default;
            
            _velocityPtr = default;
            
            _healthPtr = default;
            
        }

        public Entity Current => _current;
        object global::System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            while (true)
            {
                if (_hasChunk && _entityIndex < _chunkLength)
                {
                    _current = new Entity(
                        
                        _positionPtr,
                        
                        _velocityPtr,
                        
                        _healthPtr,
                        
                        _entityIndex);
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
                
                _currentIteration = _world.GetStorage(handle)
                    .As<global::Sparkitect.ECS.Capabilities.IChunkedIteration>();
                
                _chunkHandle = default; // Fresh per storage (pitfall 3)
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
            
            _positionPtr = (nint)_currentIteration!.GetChunkComponentData(
                ref _chunkHandle, global::TestMod.Position.Identification);
            
            _velocityPtr = (nint)_currentIteration!.GetChunkComponentData(
                ref _chunkHandle, global::TestMod.Velocity.Identification);
            
            _healthPtr = (nint)_currentIteration!.GetChunkComponentData(
                ref _chunkHandle, global::TestMod.Health.Identification);
            
        }

        public void Reset()
        {
            _storageIndex = -1;
            _currentIteration = null;
            _chunkHandle = default;
            _chunkLength = 0;
            _entityIndex = 0;
            _hasChunk = false;
            _current = default;
            
            _positionPtr = default;
            
            _velocityPtr = default;
            
            _healthPtr = default;
            
        }

        public void Dispose() { }
    }
}
