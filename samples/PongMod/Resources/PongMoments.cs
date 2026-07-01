using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Moments;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Moments;

namespace PongMod.Resources;

/// <summary>
/// PongMod's resource-moment registrations. Rides <see cref="ResourceMomentRegistry.RegisterMoment"/>
/// with zero generator work — the source generator emits <c>GraphMomentID.PongMod.Target</c> from this
/// value registration.
/// </summary>
[PublicAPI]
public static class PongMoments
{
    /// <summary>
    /// The <c>target</c> moment: cross-pass identity for the shared compute render target. The compute
    /// pass's write view publishes it; the copy pass's read view consumes it. Carries the shared image
    /// as its resource type, replacing the deprecated shared-image registry identity.
    /// </summary>
    [ResourceMomentRegistry.RegisterMoment("target")]
    public static ResourceMomentDefinition Target() => new ResourceMomentDefinition<ImageResource>();
}
