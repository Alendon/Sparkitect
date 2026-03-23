using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability interface for chunk-based iteration over storage entities.
/// Dense SoA storage exposes the entire array as a single chunk.
/// </summary>
public interface IChunkedIteration : ICapability
{
    /// <summary>
    /// Advances iteration to the next chunk. Returns <c>false</c> when iteration is complete.
    /// For dense SoA storage, the entire array is one chunk (offset=0, length=count).
    /// </summary>
    /// <param name="handle">Iteration state, modified on each call.</param>
    /// <param name="length">The number of entities in the current chunk.</param>
    /// <returns><c>true</c> if a chunk is available; <c>false</c> if iteration is complete.</returns>
    bool GetNextChunk(ref ChunkHandle handle, out int length);

    /// <summary>
    /// Returns a pointer to the raw component data for the current chunk.
    /// </summary>
    /// <param name="handle">The current iteration state.</param>
    /// <param name="componentId">The component identification to retrieve data for.</param>
    /// <returns>Pointer to the start of component data for this chunk.</returns>
    unsafe byte* GetChunkComponentData(ref ChunkHandle handle, Identification componentId);
}

/// <summary>
/// Generic extension of <see cref="IChunkedIteration"/> that provides entity key access during iteration.
/// Systems needing entity keys for command buffer operations use this via <see cref="Queries.ComponentQuery{TKey}"/>.
/// </summary>
public interface IChunkedIteration<TKey> : IChunkedIteration
    where TKey : unmanaged
{
    /// <summary>
    /// Returns the entity key for the entity at the given index within the current chunk.
    /// </summary>
    /// <param name="handle">The current iteration state.</param>
    /// <param name="index">The entity index within the current chunk.</param>
    /// <returns>The entity key (e.g., EntityId) for the entity.</returns>
    TKey GetKey(ref ChunkHandle handle, int index);
}
