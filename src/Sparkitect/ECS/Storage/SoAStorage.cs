using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.ECS.Storage;

/// <summary>
/// SoA archetype storage implementing IStorage&lt;int&gt; with capability interfaces.
/// Manages entities across multiple NativeColumns with swap-and-pop removal
/// and single-chunk dense iteration. Implements entity identity tracking via
/// <see cref="IEntityIdentity{TKey}"/> delegating to an internal <see cref="EntityIdentityMap{TKey}"/>.
/// </summary>
public sealed unsafe class SoAStorage : IStorage<int>, IChunkedIteration, IComponentAccess<int>, IEntityMutation<int>, IEntityIdentity<int>
{
    private readonly Dictionary<Identification, NativeColumn> _columns;
    private readonly IWorld _world;
    private readonly EntityIdentityMap<int> _identityMap;
    private StorageHandle _handle;
    private bool _handleSet;
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Creates a new SoA storage with columns for each component.
    /// </summary>
    /// <param name="componentMeta">Component metadata: ID, size, and alignment for each column.</param>
    /// <param name="tracker">Object tracker for leak detection of NativeColumns.</param>
    /// <param name="world">The world this storage belongs to, used for BindEntity calls.</param>
    /// <param name="initialCapacity">Initial entity capacity per column.</param>
    public SoAStorage(
        IReadOnlyList<(Identification Id, int Size, int Alignment)> componentMeta,
        IObjectTracker<IDisposable> tracker,
        IWorld world,
        int initialCapacity = 64)
    {
        _world = world;
        _identityMap = new EntityIdentityMap<int>(initialCapacity);
        _columns = new Dictionary<Identification, NativeColumn>(componentMeta.Count);
        foreach (var (id, size, alignment) in componentMeta)
        {
            _columns[id] = new NativeColumn(size, alignment, initialCapacity, tracker);
        }
    }

    /// <summary>
    /// Sets the storage handle after registration with the World.
    /// Must be called before <see cref="Assign"/> so that BindEntity can reference this storage.
    /// </summary>
    /// <param name="handle">The handle returned by <see cref="IWorld.AddStorage"/>.</param>
    public void SetHandle(StorageHandle handle)
    {
        _handle = handle;
        _handleSet = true;
    }

    /// <inheritdoc/>
    public int AllocateEntity()
    {
        foreach (var column in _columns.Values)
        {
            column.AddSlot();
        }
        return _count++;
    }

    /// <inheritdoc/>
    public ref T Get<T>(Identification componentId, int slot) where T : unmanaged
    {
        return ref _columns[componentId].Get<T>(slot);
    }

    /// <inheritdoc/>
    public void Set<T>(Identification componentId, int slot, T value) where T : unmanaged
    {
        _columns[componentId].Set(slot, value);
    }

    /// <inheritdoc/>
    public void RemoveEntity(int key)
    {
        int lastIndex = _count - 1;
        if (key != lastIndex)
        {
            _identityMap.NotifySwap(lastIndex, key);
        }

        foreach (var column in _columns.Values)
        {
            column.RemoveSlotBySwap(key);
        }
        _count--;
    }

    /// <inheritdoc/>
    public void Assign(EntityId id, int slot)
    {
        if (!_handleSet)
        {
            throw new InvalidOperationException(
                "SetHandle must be called before Assign. Register the storage with World first.");
        }

        _identityMap.Assign(id, slot);
        _world.BindEntity(id, _handle);
    }

    /// <inheritdoc/>
    public bool TryResolve(EntityId id, out int slot)
    {
        return _identityMap.TryResolve(id, out slot);
    }

    /// <inheritdoc/>
    public void Unassign(EntityId id)
    {
        _identityMap.Unassign(id);
    }

    /// <inheritdoc/>
    public EntityId GetEntityId(int slot)
    {
        return _identityMap.GetEntityId(slot);
    }

    /// <inheritdoc/>
    public bool GetNextChunk(ref ChunkHandle handle, out int length)
    {
        if (handle.Complete || _count == 0)
        {
            length = 0;
            return false;
        }

        handle.Offset = 0;
        handle.Complete = true;
        length = _count;
        return true;
    }

    /// <inheritdoc/>
    public byte* GetChunkComponentData(ref ChunkHandle handle, Identification componentId)
    {
        var column = _columns[componentId];
        return column.GetElementPtr(handle.Offset);
    }

    /// <summary>
    /// Creates capability registrations for this storage's component set and identity capability.
    /// Each capability interface gets its own registration with shared ComponentSetMetadata.
    /// </summary>
    /// <returns>Capability registrations for IChunkedIteration, IComponentAccess, IEntityMutation&lt;int&gt;, and IEntityIdentity&lt;int&gt;.</returns>
    public IReadOnlyList<CapabilityRegistration> CreateCapabilityRegistrations()
    {
        var componentIds = new HashSet<Identification>(_columns.Keys);
        var metadata = new ComponentSetMetadata(componentIds);
        return new CapabilityRegistration[]
        {
            new CapabilityRegistration<IChunkedIteration, ComponentSetMetadata>(metadata),
            new CapabilityRegistration<IComponentAccess<int>, ComponentSetMetadata>(metadata),
            new CapabilityRegistration<IEntityMutation<int>, ComponentSetMetadata>(metadata),
            new CapabilityRegistration<IEntityIdentity<int>, EntityIdentityMetadata>(new EntityIdentityMetadata()),
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var column in _columns.Values)
        {
            column.Dispose();
        }
        _columns.Clear();
    }
}
