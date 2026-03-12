using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.ECS.Storage;

/// <summary>
/// SoA archetype storage implementing IStorage&lt;int&gt; with capability interfaces.
/// Manages entities across multiple NativeColumns with swap-and-pop removal
/// and single-chunk dense iteration.
/// </summary>
public sealed unsafe class SoAStorage : IStorage<int>, IChunkedIteration, IComponentAccess<int>, IEntityMutation<int>
{
    private readonly Dictionary<Identification, NativeColumn> _columns;
    private int _count;
    private bool _disposed;

    /// <summary>
    /// Creates a new SoA storage with columns for each component.
    /// </summary>
    /// <param name="componentMeta">Component metadata: ID, size, and alignment for each column.</param>
    /// <param name="tracker">Object tracker for leak detection of NativeColumns.</param>
    /// <param name="initialCapacity">Initial entity capacity per column.</param>
    public SoAStorage(
        IReadOnlyList<(Identification Id, int Size, int Alignment)> componentMeta,
        IObjectTracker<IDisposable> tracker,
        int initialCapacity = 64)
    {
        _columns = new Dictionary<Identification, NativeColumn>(componentMeta.Count);
        foreach (var (id, size, alignment) in componentMeta)
        {
            _columns[id] = new NativeColumn(size, alignment, initialCapacity, tracker);
        }
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
        foreach (var column in _columns.Values)
        {
            column.RemoveSlotBySwap(key);
        }
        _count--;
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
    /// Creates capability registrations for this storage's component set.
    /// Each capability interface gets its own registration with shared ComponentSetMetadata.
    /// </summary>
    /// <param name="componentIds">The set of component IDs this storage manages.</param>
    /// <returns>Capability registrations for IChunkedIteration, IComponentAccess, and IEntityMutation&lt;int&gt;.</returns>
    public static IReadOnlyList<CapabilityRegistration> CreateCapabilityRegistrations(IReadOnlySet<Identification> componentIds)
    {
        var metadata = new ComponentSetMetadata(componentIds);
        return new CapabilityRegistration[]
        {
            new CapabilityRegistration<IChunkedIteration, ComponentSetMetadata>(metadata),
            new CapabilityRegistration<IComponentAccess<int>, ComponentSetMetadata>(metadata),
            new CapabilityRegistration<IEntityMutation<int>, ComponentSetMetadata>(metadata),
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
