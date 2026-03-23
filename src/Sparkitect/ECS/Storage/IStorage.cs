namespace Sparkitect.ECS.Storage;

/// <summary>
/// Non-generic base interface for all storage implementations.
/// Extends <see cref="IDisposable"/> for lifecycle management by <see cref="World"/>.
/// </summary>
public interface IStorage : IDisposable
{
    /// <summary>
    /// The number of entities currently stored.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Generic storage contract parameterized by entity key type.
/// Concrete storage implementations provide their own key allocation strategy.
/// </summary>
/// <typeparam name="TKey">The unmanaged entity key type allocated by this storage.</typeparam>
public interface IStorage<TKey> : IStorage
    where TKey : unmanaged
{
    /// <summary>
    /// Allocates a new entity within this storage and returns its key.
    /// </summary>
    /// <returns>The key identifying the newly allocated entity.</returns>
    TKey AllocateEntity();
}
