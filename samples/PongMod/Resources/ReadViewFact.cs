using PongMod.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>Builds the copy pass's read view by re-resolving the shared transient leaf through the target moment — the same N=1 instance the write view published, so both share one tracked layout state.</summary>
[FactRegistry.Register("pong_read_view")]
public sealed partial record ReadViewFact : DeclaredFact<TransferSrcReadView>, IHasIdentification
{
    /// <inheritdoc/>
    public TransferSrcReadView CreateInstance(IInstanceContext ctx)
    {
        var leaf = ctx.ResolveMoment<ImageResource>(GraphMomentID.PongMod.Target);
        return new TransferSrcReadView(leaf);
    }

    /// <summary>The view owns no GPU object; the shared transient leaf is graph-owned.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
