using Sparkitect.ECS.Commands;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

public interface ISystemManager
{
    /// <summary>
    /// Retrieves the shared command buffer accessor for the specified world.
    /// Returns null if the world cache has not been built yet.
    /// </summary>
    ICommandBufferAccessor? GetCommandBufferAccessor(IWorld world);
    void RegisterSystem(Identification id);
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

    void ExecuteSystems(IWorld world, FrameTiming frameTiming);
    void NotifyRebuild(IWorld world);
    void NotifyDispose(IWorld world);
}
