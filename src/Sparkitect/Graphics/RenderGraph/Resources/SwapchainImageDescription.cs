using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Description for a swapchain-backed present-target leaf. Declares only the plain
/// <see cref="ImageResource"/> backing and marks no moment — the finishline marking is the caller's concern.
/// </summary>
[PublicAPI]
public sealed record SwapchainImageDescription : IResourceDescription<ImageResource>
{
    /// <inheritdoc/>
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx) =>
        tx.InstantiateFact<SwapchainImageFact>();
}
