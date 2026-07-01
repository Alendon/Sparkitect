using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the compute write view. It sub-declares the shared VMA-transient image leaf and marks
/// the <c>target</c> moment on that plain <see cref="ImageResource"/> reference — the cross-pass identity
/// the copy pass's read view re-resolves through the moment — then instantiates the
/// <see cref="WriteViewFact"/> over that same leaf.
/// </summary>
[PublicAPI]
public sealed record WriteViewDescription : IResourceDescription<StorageWriteView>
{
    /// <summary>The transient target's symbolic size (matches the swapchain by default).</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The transient target's format.</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public DeclaredFact<StorageWriteView> Declare(IResourceTransaction tx)
    {
        // Sub-declare the shared transient leaf and mark the target moment on that plain ImageResource ref:
        // the target is a real image, so the moment's increment node resolves to it directly and the read
        // view re-resolves the SAME chain through the moment.
        var leafRef = tx.Declare(new TransientImageDescription { Extent = Extent, Format = Format });
        tx.Increment(leafRef, GraphMomentID.PongMod.Target);

        var fact = tx.InstantiateFact<WriteViewFact>();
        return fact with { LeafRef = leafRef };
    }
}
