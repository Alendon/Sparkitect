using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the copy pass's read view: it references the <c>target</c> moment (the cross-pass
/// identity the compute write view published) so the copy pass orders after the compute pass with no
/// explicit ordering attribute (D-01/D-15), then instantiates the <see cref="ReadViewFact"/> carrying the
/// extent intent + format so the fact re-resolves the same shared N=1 leaf as a blit source.
/// </summary>
[PublicAPI]
public sealed record ReadViewDescription : IResourceDescription<TransferSrcReadView>
{
    /// <summary>The shared target's symbolic size (must match the write view).</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The shared target's format (must match the write view).</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public DeclaredFact<TransferSrcReadView> Declare(IResourceTransaction tx)
    {
        // Reference the target moment (never increment it): the Read-after-Increment edge that — once the
        // ordering plan lands — sequences the copy pass after the compute pass. Ordering-only.
        tx.ReferenceMoment(GraphMomentID.PongMod.Target);

        var fact = tx.InstantiateFact<ReadViewFact>();
        return fact with { Extent = Extent, Format = Format };
    }
}
