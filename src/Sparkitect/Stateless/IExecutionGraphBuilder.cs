namespace Sparkitect.Stateless;

public interface IExecutionGraphBuilder<in TNode> where TNode : unmanaged
{
    void AddNode(TNode node);
    void AddEdge(TNode from, TNode to, bool optional);
    object Resolve();
}