using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Shared engine description for a VMA-transient image leaf. It declares only the plain
/// <see cref="ImageResource"/> backing, carrying the transient sizing intent, and marks no moment — the
/// compute write view that sub-declares it marks the target moment on the reference it returns.
/// </summary>
[PublicAPI]
public sealed record TransientImageDescription : IResourceDescription<ImageResource>
{
    /// <summary>The transient leaf's symbolic size (matches the swapchain by default).</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The transient leaf's format.</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx)
    {
        var fact = tx.InstantiateFact<TransientImageFact>();
        return fact with { Extent = Extent, Format = Format };
    }
}
