using Sparkitect.Modding;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Output of <see cref="RenderGraphCompiler.Compile"/> — an ordered list of
/// <c>(Identification, IPass)</c> pairs consumed by <c>RenderGraph.RunFrame</c>
/// to dispatch <c>IExecuteHook.Execute</c> in topo-sorted order.
/// </summary>
internal sealed class CompiledRenderGraph
{
    public IReadOnlyList<(Identification Id, IPass Pass)> OrderedPasses { get; }

    public CompiledRenderGraph(IReadOnlyList<(Identification Id, IPass Pass)> orderedPasses)
    {
        OrderedPasses = orderedPasses;
    }
}
