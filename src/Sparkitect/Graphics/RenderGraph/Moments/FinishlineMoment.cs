using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Moments;

namespace Sparkitect.Graphics.RenderGraph.Moments;

/// <summary>
/// The render-graph-reserved finishline moment: the single cross-pass mark a pass uses to signal the
/// presentation target is finalized. It rides the existing moment registry with no generator changes —
/// the source generator emits its <c>Identification</c> from this value registration. The pass that
/// finalizes the target increments its image and marks that increment with the finishline; the graph
/// references the moment as a consumer and issues present at the bound position.
/// </summary>
public static class FinishlineMoment
{
    /// <summary>The finishline moment definition: it carries the swapchain-backed image as its resource type.</summary>
    [ResourceMomentRegistry.RegisterMoment("finishline")]
    public static ResourceMomentDefinition Finishline() => new ResourceMomentDefinition<ImageResource>();
}
