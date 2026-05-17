using Sparkitect.ECS.Capabilities;
using Sparkitect.Modding;
using Serilog;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Abstract non-generic command buffer base. Holds the command list, entity reference,
/// and provides user-facing recording methods. Subclassed by CommandBuffer&lt;TKey&gt;
/// which handles playback with the concrete storage key type.
/// </summary>
public abstract class CommandBuffer : ICommandBuffer
{
    /// <inheritdoc/>
    public EntityId EntityId { get; }

    /// <inheritdoc/>
    public StorageHandle StorageHandle { get; }

    /// <summary>The recorded commands to execute at playback.</summary>
    internal List<ICommand> Commands { get; } = new();

    /// <summary>Whether this buffer represents entity creation (vs modification).</summary>
    internal bool IsCreate { get; }

    protected CommandBuffer(EntityId entityId, StorageHandle storageHandle, bool isCreate)
    {
        EntityId = entityId;
        StorageHandle = storageHandle;
        IsCreate = isCreate;
    }

    /// <inheritdoc/>
    public void SetComponent<T>(T value) where T : unmanaged, IHasIdentification
        => Commands.Add(new SetComponentCommand<T>(value));

    /// <inheritdoc/>
    public void DestroyEntity()
        => Commands.Add(new DestroyEntityCommand());

    /// <summary>
    /// Executes the create preamble (if applicable) and all recorded commands.
    /// Buffer-level entity validation per D-17: resolve once, pass to all commands.
    /// </summary>
    internal abstract void PlaybackCommands(IWorld world);
}

/// <summary>
/// Sealed generic command buffer that handles TKey-specific playback.
/// Created by CommandBufferAccessor via MakeGenericType (D-15).
/// </summary>
internal sealed class CommandBuffer<TKey> : CommandBuffer where TKey : unmanaged
{
    internal CommandBuffer(EntityId entityId, StorageHandle storageHandle, bool isCreate)
        : base(entityId, storageHandle, isCreate) { }

    /// <inheritdoc/>
    internal override void PlaybackCommands(IWorld world)
    {
        var accessor = world.GetStorage(StorageHandle);

        if (IsCreate)
        {
            var storage = accessor.AsStorage<TKey>()!;
            var slot = storage.AllocateEntity();
            var identity = accessor.As<IEntityIdentity<TKey>>()!;
            identity.Assign(EntityId, slot);
            foreach (var command in Commands)
                command.Execute<TKey>(world, StorageHandle, slot);
        }
        else
        {
            // Buffer-level validation per D-17: resolve once
            var identity = accessor.As<IEntityIdentity<TKey>>()!;
            if (!identity.TryResolve(EntityId, out var slot))
            {
                // Entity destroyed by prior buffer -- drop entire buffer with warning
                Log.Warning("Command buffer for entity {EntityId} dropped: entity no longer assigned", EntityId);
                return;
            }
            foreach (var command in Commands)
                command.Execute<TKey>(world, StorageHandle, slot);
        }
    }
}
