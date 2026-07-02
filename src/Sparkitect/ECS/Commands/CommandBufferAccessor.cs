using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Reflection;
using Sparkitect.ECS.Capabilities;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Concrete command buffer accessor implementation.
/// Uses IStorage.KeyType + MakeGenericType to create typed CommandBuffer&lt;TKey&gt;
/// instances without exposing TKey to callers.
/// </summary>
[PublicAPI]
public class CommandBufferAccessor : ICommandBufferAccessor
{
    private readonly IWorld _world;
    private readonly List<CommandBuffer> _buffers = new();

    // Cache: KeyType -> constructor. Avoids repeated MakeGenericType + reflection.
    // Key space is tiny (typically 1 distinct TKey: int). Thread-safe for parallel access.
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = new();

    /// <summary>Creates an accessor that records command buffers against the given world.</summary>
    public CommandBufferAccessor(IWorld world)
    {
        _world = world;
    }

    /// <inheritdoc/>
    public ICommandBuffer Create(StorageHandle storageHandle)
    {
        var entityId = _world.AllocateEntityId();
        var buffer = CreateBuffer(storageHandle, entityId, isCreate: true);
        return buffer;
    }

    /// <inheritdoc/>
    public ICommandBuffer Create(IReadOnlyList<ICapabilityRequirement> filter)
    {
        var handles = _world.Resolve(filter);
        if (handles.Count == 0)
        {
            throw new InvalidOperationException(
                "No storage matched the provided filter. Cannot create entity without a target storage.");
        }
        return Create(handles[0]);
    }

    /// <inheritdoc/>
    public ICommandBuffer Modify(EntityId entityId)
    {
        var state = _world.GetEntityState(entityId);
        if (state != EntityState.Bound)
        {
            throw new InvalidOperationException(
                $"Cannot modify entity {entityId}: expected state Bound but found {state}.");
        }

        var storageHandle = _world.GetStorageHandle(entityId);
        var buffer = CreateBuffer(storageHandle, entityId, isCreate: false);
        return buffer;
    }

    /// <inheritdoc/>
    public void Playback()
    {
        foreach (var buffer in _buffers)
        {
            buffer.PlaybackCommands(_world);
        }
        _buffers.Clear();
    }

    private CommandBuffer CreateBuffer(StorageHandle storageHandle, EntityId entityId, bool isCreate)
    {
        // Resolve TKey at runtime via IStorage.KeyType (D-16)
        var storageAccessor = _world.GetStorage(storageHandle);
        var keyType = storageAccessor.KeyType;

        var ctor = ConstructorCache.GetOrAdd(keyType, static kt =>
        {
            // MakeGenericType -- used once per distinct TKey (D-15)
            var bufferType = typeof(CommandBuffer<>).MakeGenericType(kt);
            return bufferType.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                [typeof(EntityId), typeof(StorageHandle), typeof(bool)],
                null)!;
        });

        var buffer = (CommandBuffer)ctor.Invoke([entityId, storageHandle, isCreate]);
        _buffers.Add(buffer);
        return buffer;
    }
}
