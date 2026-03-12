namespace Sparkitect.ECS.Storage;

/// <summary>
/// Reusable bidirectional map between <see cref="EntityId"/> and storage-internal slot keys.
/// Forward arrays are indexed by <see cref="EntityId.Index"/> (dense from the World pool).
/// Reverse lookup uses a dictionary since slot keys become sparse after removals.
/// </summary>
/// <typeparam name="TKey">The storage-internal key type.</typeparam>
public class EntityIdentityMap<TKey>
    where TKey : unmanaged
{
    private TKey[] _entityToKey;
    private bool[] _assigned;
    private readonly Dictionary<TKey, EntityId> _keyToEntity;

    /// <summary>
    /// Creates a new identity map with the specified initial forward array capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial size of forward arrays (indexed by EntityId.Index).</param>
    public EntityIdentityMap(int initialCapacity = 64)
    {
        _entityToKey = new TKey[initialCapacity];
        _assigned = new bool[initialCapacity];
        _keyToEntity = new Dictionary<TKey, EntityId>();
    }

    /// <summary>
    /// Assigns a bidirectional mapping between an entity ID and a slot key.
    /// Grows forward arrays on demand if the entity index exceeds current capacity.
    /// </summary>
    public void Assign(EntityId id, TKey key)
    {
        EnsureCapacity(id.Index);
        _entityToKey[id.Index] = key;
        _assigned[id.Index] = true;
        _keyToEntity[key] = id;
    }

    /// <summary>
    /// Attempts to resolve the slot key for the given entity ID.
    /// </summary>
    public bool TryResolve(EntityId id, out TKey key)
    {
        if (id.Index < (uint)_assigned.Length && _assigned[id.Index])
        {
            key = _entityToKey[id.Index];
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Removes the mapping for the given entity ID.
    /// </summary>
    public void Unassign(EntityId id)
    {
        var key = _entityToKey[id.Index];
        _keyToEntity.Remove(key);
        _assigned[id.Index] = false;
    }

    /// <summary>
    /// Returns the entity ID mapped to the given slot key.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The key has no entity mapping.</exception>
    public EntityId GetEntityId(TKey key)
    {
        return _keyToEntity[key];
    }

    /// <summary>
    /// Notifies the map that a slot has been moved (e.g., during swap-and-pop removal).
    /// Updates both forward and reverse mappings so the entity at <paramref name="from"/>
    /// is now at <paramref name="to"/>.
    /// </summary>
    /// <param name="from">The old slot key being vacated.</param>
    /// <param name="to">The new slot key the entity now occupies.</param>
    public void NotifySwap(TKey from, TKey to)
    {
        if (!_keyToEntity.TryGetValue(from, out var entity))
            return;

        _keyToEntity.Remove(from);
        _keyToEntity[to] = entity;
        _entityToKey[entity.Index] = to;
    }

    private void EnsureCapacity(uint index)
    {
        if (index < (uint)_entityToKey.Length)
            return;

        var newCapacity = (int)(index + 1);
        // Round up to next power of 2 for amortized growth
        newCapacity = (int)uint.Max((uint)newCapacity, (uint)_entityToKey.Length * 2);

        Array.Resize(ref _entityToKey, newCapacity);
        Array.Resize(ref _assigned, newCapacity);
    }
}
