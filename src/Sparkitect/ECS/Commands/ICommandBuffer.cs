using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Non-generic buffer interface for recording deferred mutations against a single entity.
/// System authors interact with this interface -- TKey is never exposed.
/// </summary>
[PublicAPI]
public interface ICommandBuffer
{
    /// <summary>The entity this buffer targets.</summary>
    EntityId EntityId { get; }

    /// <summary>The storage handle this buffer targets for playback.</summary>
    StorageHandle StorageHandle { get; }

    /// <summary>
    /// Records a SetComponent command to set a component value at playback.
    /// </summary>
    void SetComponent<T>(T value) where T : unmanaged, IHasIdentification;

    /// <summary>
    /// Records a DestroyEntity command to destroy this entity at playback.
    /// </summary>
    void DestroyEntity();
}
