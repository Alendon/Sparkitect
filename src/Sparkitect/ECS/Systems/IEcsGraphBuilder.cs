using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

public interface IEcsGraphBuilder
{
    /// <summary>
    /// Builds the execution graph by walking the system tree.
    /// Groups become active graph nodes with implicit parent-to-child edges.
    /// Ordering constraints from metadata are applied as additional edges.
    /// </summary>
    void BuildFromTree(
        SystemTreeNode root,
        IReadOnlyDictionary<Identification, IScheduling> systemMetadata,
        IReadOnlyDictionary<Identification, SystemGroupScheduling> groupMetadata);

    /// <summary>
    /// Resolves the graph into a sorted execution plan with group skip metadata.
    /// </summary>
    EcsExecutionGraph Resolve();
}
