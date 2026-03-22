namespace Sparkitect.ECS.Commands;

/// <summary>
/// Delegate-based fallback command type for quick one-off operations.
/// Wraps an action delegate as an <see cref="ICommand"/>.
/// </summary>
public class DelegateCommand(Action<IWorld, StorageHandle, EntityId> action) : ICommand
{
    /// <inheritdoc/>
    public void Execute(IWorld world, StorageHandle storageHandle, EntityId entityId)
        => action(world, storageHandle, entityId);
}
