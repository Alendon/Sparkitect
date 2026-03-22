using Sparkitect.Modding;

namespace Sparkitect.ECS.Commands;

/// <summary>
/// Extension methods on <see cref="ICommandBuffer{TKey}"/> for recording deferred mutations.
/// Mods can define new extensions + command types to add operations (D-03).
/// </summary>
public static class CommandBufferExtensions
{
    extension<TKey>(ICommandBuffer<TKey> buffer) where TKey : unmanaged
    {
        /// <summary>
        /// Records a SetComponent command to set a component value on the buffer's entity at playback.
        /// </summary>
        /// <typeparam name="T">The unmanaged component type.</typeparam>
        /// <param name="compId">The component identification.</param>
        /// <param name="value">The component value to set.</param>
        public void SetComponent<T>(Identification compId, T value) where T : unmanaged
        {
            buffer.Commands.Add(new SetComponentCommand<TKey, T>(compId, value));
        }

        /// <summary>
        /// Records a DestroyEntity command to destroy the buffer's entity at playback.
        /// </summary>
        public void DestroyEntity()
        {
            buffer.Commands.Add(new DestroyEntityCommand<TKey>());
        }
    }
}
