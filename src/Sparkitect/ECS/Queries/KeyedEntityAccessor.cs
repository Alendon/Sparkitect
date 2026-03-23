using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Per-entity accessor wrapping <see cref="EntityAccessor"/> with an associated entity key.
/// Used by <see cref="ComponentQuery{TKey}"/> to expose keys alongside component data.
/// </summary>
public readonly struct KeyedEntityAccessor<TKey>
    where TKey : unmanaged
{
    private readonly EntityAccessor _accessor;

    /// <summary>
    /// The entity key (e.g., EntityId) for this entity.
    /// </summary>
    public TKey Key { get; }

    internal KeyedEntityAccessor(EntityAccessor accessor, TKey key)
    {
        _accessor = accessor;
        Key = key;
    }

    /// <summary>
    /// Returns a mutable reference to the component value for this entity.
    /// </summary>
    public ref T GetRef<T>() where T : unmanaged, IHasIdentification
        => ref _accessor.GetRef<T>();

    /// <summary>
    /// Returns a copy of the component value for this entity.
    /// </summary>
    public T Get<T>() where T : unmanaged, IHasIdentification
        => _accessor.Get<T>();
}
