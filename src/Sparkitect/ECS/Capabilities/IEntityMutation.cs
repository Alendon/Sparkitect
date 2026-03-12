namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability interface for entity removal, generic on key type.
/// Phase 28 uses <see cref="IEntityMutation{TKey}"/> with <c>int</c> for internal slot keys.
/// Phase 29 will extend with generational EntityId key.
/// </summary>
/// <typeparam name="TKey">The unmanaged entity key type.</typeparam>
public interface IEntityMutation<TKey> : ICapability
    where TKey : unmanaged
{
    /// <summary>
    /// Removes the entity identified by the given key using swap-and-pop.
    /// </summary>
    /// <param name="key">The entity key to remove.</param>
    void RemoveEntity(TKey key);
}
