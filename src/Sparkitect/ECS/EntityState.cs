namespace Sparkitect.ECS;

/// <summary>
/// Three-state lifecycle for an entity within a <see cref="World"/>.
/// </summary>
public enum EntityState
{
    /// <summary>
    /// Slot has never been allocated, or has been reclaimed. The entity does not exist.
    /// </summary>
    Null = 0,

    /// <summary>
    /// An EntityId has been allocated but not yet bound to a storage.
    /// </summary>
    Empty = 1,

    /// <summary>
    /// The entity is bound to a storage and is fully alive.
    /// </summary>
    Bound = 2,
}
