using JetBrains.Annotations;
using PongMod.CompilerGenerated.IdExtensions;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the compute write view: it births the shared target leaf and publishes the
/// <c>target</c> moment (cross-pass identity for the shared image), then instantiates the
/// <see cref="WriteViewFact"/> carrying the extent intent + format so the fact resolves a VMA-transient
/// leaf sized to the swapchain.
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
        // Birth the write view and publish the target moment on its increment (D-01): the shared image's
        // cross-pass identity, consumed by the copy pass's read view.
        tx.Increment(tx.Self<StorageWriteView>(), GraphMomentID.PongMod.Target);

        // The DI keyed factory builds the fact without per-declaration data; flow the extent + format in.
        var fact = tx.InstantiateFact<WriteViewFact>();
        return fact with { Extent = Extent, Format = Format };
    }
}
