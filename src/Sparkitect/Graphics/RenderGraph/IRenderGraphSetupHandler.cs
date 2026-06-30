using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Handler that performs all post-construction setup work for a render graph: per-pass Setup
/// invocation, graph compilation, and Vulkan primitive creation. Invoked by the render-graph manager
/// exactly once after the graph instance is resolved.
/// </summary>
[PublicAPI]
public interface IRenderGraphSetupHandler
{
    /// <summary>
    /// Run the setup step with the supplied <paramref name="passIds"/> and target
    /// <paramref name="window"/>. Throws <see cref="InvalidOperationException"/> if any required
    /// graph-side invariant is violated.
    /// </summary>
    void Setup(IEnumerable<Identification> passIds, ISparkitWindow window);
}
