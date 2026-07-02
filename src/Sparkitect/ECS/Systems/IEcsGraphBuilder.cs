using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[PublicAPI]
public interface IEcsGraphBuilder
{
    /// <summary>
    /// Builds the execution graph by walking the system tree.
    /// Each group sorts its direct children locally from the metadata ordering constraints
    /// (siblings only); child group spans splice in depth-first, keeping members contiguous.
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
