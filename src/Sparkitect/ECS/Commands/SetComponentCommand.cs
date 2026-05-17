using JetBrains.Annotations;
using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Typed command that sets a component value on the targeted entity at playback time.
/// Receives the pre-resolved slot from the buffer -- no per-command TryResolve.
/// </summary>
/// <typeparam name="T">The unmanaged component type.</typeparam>
[PublicAPI]
public class SetComponentCommand<T> : ICommand
    where T : unmanaged, IHasIdentification
{
    private readonly T _value;

    public SetComponentCommand(T value) => _value = value;

    /// <inheritdoc/>
    public void Execute<TKey>(IWorld world, StorageHandle storageHandle, TKey resolvedSlot)
        where TKey : unmanaged
    {
        var accessor = world.GetStorage(storageHandle);
        var componentAccess = accessor.As<IComponentAccess<TKey>>()!;
        componentAccess.Set(resolvedSlot, _value);
    }
}
