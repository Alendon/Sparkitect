using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Handler that performs all post-construction setup work for a render graph: pass keyed-factory
/// build, per-pass Setup invocation, graph compilation, Vulkan primitive creation. Invoked by
/// the render graph manager exactly once after the graph instance is resolved from the per-graph
/// child container.
/// </summary>
[PublicAPI]
public interface IRenderGraphSetupHandler
{
    /// <summary>
    /// Run the setup step with the supplied <paramref name="passIds"/> and target
    /// <paramref name="window"/>. Throws <see cref="InvalidOperationException"/> if any required
    /// graph-side invariant is violated (unknown pass id, DI binding missing, Vulkan primitive
    /// creation failure).
    /// </summary>
    void Setup(IEnumerable<Identification> passIds, ISparkitWindow window);
}
