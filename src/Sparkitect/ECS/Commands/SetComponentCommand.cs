using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Typed command that sets a component value on the targeted entity at playback time.
/// Resolves EntityId to TKey via identity capability, then dispatches through IComponentAccess.
/// </summary>
/// <typeparam name="TKey">The unmanaged storage key type.</typeparam>
/// <typeparam name="T">The unmanaged component type.</typeparam>
public class SetComponentCommand<TKey, T> : ICommand
    where TKey : unmanaged
    where T : unmanaged
{
    private readonly Identification _componentId;
    private readonly T _value;

    public SetComponentCommand(Identification componentId, T value)
    {
        _componentId = componentId;
        _value = value;
    }

    /// <inheritdoc/>
    public void Execute(IWorld world, StorageHandle storageHandle, EntityId entityId)
    {
        var accessor = world.GetStorage(storageHandle);
        var identity = accessor.As<IEntityIdentity<TKey>>()!;
        identity.TryResolve(entityId, out var slot);
        var componentAccess = accessor.As<IComponentAccess<TKey>>()!;
        componentAccess.Set(_componentId, slot, _value);
    }
}
