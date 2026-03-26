namespace Sparkitect.ECS.Commands;

/// <summary>
/// Delegate-based fallback command type for quick one-off operations.
/// Wraps an action delegate as an <see cref="ICommand"/>.
/// The delegate receives world and storage handle; the resolved slot is available
/// but type-erased (the delegate operates at the entity level, not slot level).
/// </summary>
public class DelegateCommand(Action<IWorld, StorageHandle> action) : ICommand
{
    /// <inheritdoc/>
    public void Execute<TKey>(IWorld world, StorageHandle storageHandle, TKey resolvedSlot)
        where TKey : unmanaged
        => action(world, storageHandle);
}
