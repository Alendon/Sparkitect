using System.Runtime.InteropServices;
using Sparkitect.Utils;

namespace Sparkitect.ECS.Storage;

/// <summary>
/// Non-generic NativeMemory-backed column for storing unmanaged component data.
/// Parameterized by element size and alignment at construction time.
/// Uses AllocZeroed/Realloc/Free for simplicity.
/// </summary>
public sealed unsafe class NativeColumn : IDisposable
{
    private byte* _data;
    private int _count;
    private int _capacity;
    private readonly int _elementSize;
    private bool _disposed;
    private IObjectTracker<IDisposable>.Handle _trackerHandle;

    /// <summary>
    /// The number of active elements in this column.
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// The current capacity (number of elements that fit without reallocation).
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// The byte size of each element.
    /// </summary>
    public int ElementSize => _elementSize;

    /// <summary>
    /// Creates a new NativeColumn backed by NativeMemory.
    /// </summary>
    /// <param name="elementSize">Byte size of each element.</param>
    /// <param name="initialCapacity">Initial number of elements to allocate.</param>
    /// <param name="tracker">Object tracker for leak detection.</param>
    public NativeColumn(int elementSize, int initialCapacity, IObjectTracker<IDisposable> tracker)
    {
        _elementSize = elementSize;
        _capacity = initialCapacity;
        _count = 0;
        _data = (byte*)NativeMemory.AllocZeroed((nuint)initialCapacity, (nuint)elementSize);
        _trackerHandle = tracker.Track(this);
    }

    /// <summary>
    /// Returns a pointer to the element at the given index.
    /// </summary>
    internal byte* GetElementPtr(int index) => _data + (index * _elementSize);

    /// <summary>
    /// Returns a typed reference to the element at the given index.
    /// </summary>
    public ref T Get<T>(int index) where T : unmanaged => ref *(T*)GetElementPtr(index);

    /// <summary>
    /// Sets the typed value at the given index.
    /// </summary>
    public void Set<T>(int index, T value) where T : unmanaged => *(T*)GetElementPtr(index) = value;

    /// <summary>
    /// Copies the element at <paramref name="from"/> to <paramref name="to"/>.
    /// </summary>
    public void SwapRemove(int to, int from)
    {
        Buffer.MemoryCopy(GetElementPtr(from), GetElementPtr(to), _elementSize, _elementSize);
    }

    /// <summary>
    /// Adds a new slot, growing if necessary. Returns the index of the new slot.
    /// </summary>
    public int AddSlot()
    {
        _count++;
        EnsureCapacity(_count);
        return _count - 1;
    }

    /// <summary>
    /// Removes the element at <paramref name="index"/> using swap-and-pop.
    /// If the index is not the last element, copies the last element into the removed slot.
    /// </summary>
    public void RemoveSlotBySwap(int index)
    {
        int lastIndex = _count - 1;
        if (index != lastIndex)
        {
            SwapRemove(index, lastIndex);
        }
        _count--;
    }

    /// <summary>
    /// Ensures the column can hold at least <paramref name="required"/> elements.
    /// Doubles capacity (minimum <paramref name="required"/>) and zeroes newly allocated memory.
    /// </summary>
    public void EnsureCapacity(int required)
    {
        if (required <= _capacity) return;

        int newCapacity = Math.Max(required, _capacity * 2);
        nuint oldByteSize = (nuint)_capacity * (nuint)_elementSize;
        nuint newByteSize = (nuint)newCapacity * (nuint)_elementSize;

        _data = (byte*)NativeMemory.Realloc(_data, newByteSize);
        NativeMemory.Clear(_data + oldByteSize, newByteSize - oldByteSize);
        _capacity = newCapacity;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _trackerHandle.Free();
        NativeMemory.Free(_data);
        _data = null;
    }
}
