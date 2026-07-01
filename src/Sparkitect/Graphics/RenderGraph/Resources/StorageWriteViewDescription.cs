using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declares a compute write view: sub-declares a shared transient leaf and marks the caller-supplied target moment on it — the cross-pass identity a downstream read view re-resolves. The moment is a parameter so any sample supplies its own target.</summary>
[PublicAPI]
public sealed record StorageWriteViewDescription : IResourceDescription<StorageWriteView>
{
    /// <summary>The target moment marked on the leaf; a read view re-resolves the same chain through it.</summary>
    public required Identification TargetMoment { get; init; }

    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public DeclaredFact<StorageWriteView> Declare(IResourceTransaction tx)
    {
        // The target is a real image, so the increment resolves to it directly and a read view re-resolves the same chain.
        var leafRef = tx.Declare(new TransientImageDescription { Extent = Extent, Format = Format });
        tx.Increment(leafRef, TargetMoment);

        var fact = tx.InstantiateFact<StorageWriteViewFact>();
        return fact with { LeafRef = leafRef };
    }
}
