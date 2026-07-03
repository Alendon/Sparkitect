using JetBrains.Annotations;

namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability interface for entity removal, generic on key type.
/// </summary>
/// <typeparam name="TKey">The unmanaged entity key type.</typeparam>
[PublicAPI]
public interface IEntityMutation<TKey> : ICapability
    where TKey : unmanaged
{
    /// <summary>
    /// Removes the entity identified by the given key using swap-and-pop.
    /// </summary>
    /// <param name="key">The entity key to remove.</param>
    void RemoveEntity(TKey key);
}
