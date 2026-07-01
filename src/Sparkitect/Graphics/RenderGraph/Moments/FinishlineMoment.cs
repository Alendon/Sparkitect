using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Moments;

namespace Sparkitect.Graphics.RenderGraph.Moments;

/// <summary>
/// The render-graph-reserved finishline moment: the single cross-pass mark a pass uses to signal the
/// presentation target is finalized. The graph references it as a consumer and presents at the bound position.
/// </summary>
public static class FinishlineMoment
{
    /// <summary>The finishline moment definition; carries the swapchain-backed image as its resource type.</summary>
    [ResourceMomentRegistry.RegisterMoment("finishline")]
    public static ResourceMomentDefinition Finishline() => new ResourceMomentDefinition<ImageResource>();
}
