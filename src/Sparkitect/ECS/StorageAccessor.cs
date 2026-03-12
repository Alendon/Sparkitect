using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;

namespace Sparkitect.ECS;

/// <summary>
/// Stack-only wrapper providing safe access to a storage instance.
/// Obtained from <see cref="World"/>. Cannot be stored in fields or closures --
/// the <c>ref struct</c> constraint enforces this at the language level.
/// </summary>
public readonly ref struct StorageAccessor
{
    private readonly IStorage _storage;

    /// <summary>
    /// The handle identifying the storage this accessor wraps.
    /// </summary>
    public readonly StorageHandle Handle;

    internal StorageAccessor(IStorage storage, StorageHandle handle)
    {
        _storage = storage;
        Handle = handle;
    }

    /// <summary>
    /// Attempts to cast the underlying storage to a capability interface.
    /// Returns <c>null</c> if the storage does not implement the requested capability.
    /// </summary>
    /// <remarks>
    /// The returned capability reference CAN escape the ref struct scope.
    /// Do not cache capability references obtained this way -- they remain valid
    /// only as long as the storage is registered with the World.
    /// </remarks>
    /// <typeparam name="TCapability">The capability interface to cast to.</typeparam>
    /// <returns>The storage as the requested capability, or <c>null</c> if not supported.</returns>
    public TCapability? As<TCapability>() where TCapability : class, ICapability
    {
        return _storage as TCapability;
    }

    /// <summary>
    /// Attempts to cast the underlying storage to a typed <see cref="IStorage{TKey}"/>.
    /// Returns <c>null</c> if the storage does not use the specified key type.
    /// </summary>
    /// <typeparam name="TKey">The unmanaged entity key type.</typeparam>
    /// <returns>The typed storage, or <c>null</c> if the key type does not match.</returns>
    public IStorage<TKey>? AsStorage<TKey>() where TKey : unmanaged
    {
        return _storage as IStorage<TKey>;
    }
}
