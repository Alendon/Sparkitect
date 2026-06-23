using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Runtime;

/// <summary>
/// Thin <see cref="IExecutionGraphBuilder"/> adapter over <see cref="RenderGraphCompiler"/> so the
/// shared <see cref="OrderAfterAttribute.Apply"/> / <see cref="OrderBeforeAttribute.Apply"/> logic
/// feeds ordering edges straight into the compiler. Passes are already registered via
/// <see cref="RenderGraphCompiler.AddPass"/>, so <see cref="AddNode"/> is a no-op; the optional flag
/// is dropped because the compiler models hard edges only.
/// </summary>
internal sealed class RenderGraphOrderingBuilder : IExecutionGraphBuilder
{
    private readonly RenderGraphCompiler _compiler;

    public RenderGraphOrderingBuilder(RenderGraphCompiler compiler)
    {
        _compiler = compiler;
    }

    public void AddNode(Identification node)
    {
        // No-op: passes are added through RenderGraphCompiler.AddPass during Setup.
    }

    public void AddEdge(Identification from, Identification to, bool optional)
    {
        _compiler.AddOrderingEdgeInternal(from, to);
    }

    public IReadOnlyList<Identification> Resolve() =>
        throw new NotSupportedException(
            "RenderGraphOrderingBuilder does not resolve ordering — RenderGraphCompiler.Compile() is the resolver.");
}
