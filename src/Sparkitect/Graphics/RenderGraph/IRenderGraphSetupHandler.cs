using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.Modding;
using Sparkitect.Windowing;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Performs all post-construction setup for a render graph (per-pass Setup, compilation, Vulkan primitive
/// creation). Invoked once after the graph instance is resolved.
/// </summary>
[PublicAPI]
public interface IRenderGraphSetupHandler
{
    /// <summary>
    /// Run setup with <paramref name="passIds"/> and target <paramref name="window"/>, resolving passes
    /// and facts against the per-graph <paramref name="graphContainer"/>.
    /// </summary>
    void Setup(IEnumerable<Identification> passIds, ISparkitWindow window, ICoreContainer graphContainer);
}
