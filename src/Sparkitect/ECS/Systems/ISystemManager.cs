using JetBrains.Annotations;
using Sparkitect.ECS.Commands;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Coordinates ECS system and group registration, metadata collection, per-world execution caching,
/// and frame execution. Registered as a state service on the ECS module.
/// </summary>
[PublicAPI]
public interface ISystemManager
{
    /// <summary>
    /// Retrieves the shared command buffer accessor for the specified world.
    /// Returns null if the world cache has not been built yet.
    /// </summary>
    ICommandBufferAccessor? GetCommandBufferAccessor(IWorld world);

    /// <summary>Marks a system id as registered so it participates in tree building.</summary>
    void RegisterSystem(Identification id);

    /// <summary>Marks a system-group id as registered so it participates in tree building.</summary>
    void RegisterSystemGroup(Identification id);

    /// <summary>
    /// Eagerly collects all system and group metadata from registered entrypoints.
    /// Must be called after registration pass execution and before BuildTree.
    /// </summary>
    void FetchMetadata();

    /// <summary>
    /// Builds a SystemTreeNode tree rooted at the specified group from cached metadata.
    /// Systems and groups not reachable from the root are excluded.
    /// </summary>
    /// <param name="rootGroupId">The root group identification.</param>
    /// <returns>The root node of the constructed tree.</returns>
    SystemTreeNode BuildTree(Identification rootGroupId);

    /// <summary>Executes the world's active systems in resolved order for one frame, building the world cache on first use.</summary>
    void ExecuteSystems(IWorld world, FrameTiming frameTiming);

    /// <summary>Discards the world's cached execution state so it rebuilds on the next frame (e.g. after topology change).</summary>
    void NotifyRebuild(IWorld world);

    /// <summary>Releases the world's cached execution state and its tracked queries when the world is disposed.</summary>
    void NotifyDispose(IWorld world);
}
