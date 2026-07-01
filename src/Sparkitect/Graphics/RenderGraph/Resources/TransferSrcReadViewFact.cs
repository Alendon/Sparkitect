using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Builds the read view by re-resolving the shared transient leaf through the target moment — the same N=1 instance the write view published, so both share one tracked layout state.</summary>
[FactRegistry.Register("transfer_src_read_view")]
public sealed partial record TransferSrcReadViewFact : DeclaredFact<TransferSrcReadView>, IHasIdentification
{
    /// <summary>The target moment resolved to the shared leaf, flowed in from the description.</summary>
    public Identification TargetMoment { get; init; }

    /// <inheritdoc/>
    public TransferSrcReadView CreateInstance(IInstanceContext ctx)
    {
        var leaf = ctx.ResolveMoment<ImageResource>(TargetMoment);
        return new TransferSrcReadView(leaf);
    }

    /// <summary>The view owns no GPU object; the shared transient leaf is graph-owned.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
