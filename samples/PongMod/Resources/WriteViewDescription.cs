using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>Declares the compute write view: sub-declares the shared transient leaf and marks the <c>target</c> moment on it — the cross-pass identity the copy pass's read view re-resolves.</summary>
[PublicAPI]
public sealed record WriteViewDescription : IResourceDescription<StorageWriteView>
{
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public DeclaredFact<StorageWriteView> Declare(IResourceTransaction tx)
    {
        // Mark the target moment on the plain leaf ref: the target is a real image, so the increment
        // resolves to it directly and the read view re-resolves the same chain.
        var leafRef = tx.Declare(new TransientImageDescription { Extent = Extent, Format = Format });
        tx.Increment(leafRef, GraphMomentID.PongMod.Target);

        var fact = tx.InstantiateFact<WriteViewFact>();
        return fact with { LeafRef = leafRef };
    }
}
