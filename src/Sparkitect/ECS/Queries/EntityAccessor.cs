using System.Runtime.CompilerServices;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Per-entity accessor exposing component data via dictionary lookup.
/// Holds pointers to component data for the current chunk and the entity's index within it.
/// The dictionary lookup per Get/GetRef is acceptable for v1.4 -- v1.5 SG eliminates it.
/// </summary>
public readonly unsafe struct EntityAccessor
{
    private readonly Dictionary<Identification, nint> _componentPointers;
    private readonly int _index;

    internal EntityAccessor(Dictionary<Identification, nint> componentPointers, int index)
    {
        _componentPointers = componentPointers;
        _index = index;
    }

    /// <summary>
    /// Returns a mutable reference to the component value for this entity.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type.</typeparam>
    /// <returns>A reference to the component value.</returns>
    public ref T GetRef<T>() where T : unmanaged, IHasIdentification
    {
        var basePtr = _componentPointers[T.Identification];
        return ref Unsafe.AsRef<T>((void*)(basePtr + _index * sizeof(T)));
    }

    /// <summary>
    /// Returns a copy of the component value for this entity.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type.</typeparam>
    /// <returns>A copy of the component value.</returns>
    public T Get<T>() where T : unmanaged, IHasIdentification
    {
        return GetRef<T>();
    }
}
