namespace Sparkitect.ECS.Storage;

/// <summary>
/// Iteration state for chunk-based traversal of storage entities.
/// Default-constructed starts at offset 0, not complete.
/// </summary>
public struct ChunkHandle
{
    /// <summary>
    /// The current offset within the storage data.
    /// </summary>
    internal int Offset;

    /// <summary>
    /// Whether iteration has completed.
    /// </summary>
    internal bool Complete;
}
