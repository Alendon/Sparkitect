using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Moments;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Moments;

namespace PongMod.Resources;

/// <summary>PongMod's resource-moment registrations.</summary>
[PublicAPI]
public static class PongMoments
{
    /// <summary>The <c>target</c> moment: cross-pass identity for the shared compute render target — the write view publishes it, the read view consumes it.</summary>
    [ResourceMomentRegistry.RegisterMoment("target")]
    public static ResourceMomentDefinition Target() => new ResourceMomentDefinition<ImageResource>();
}
