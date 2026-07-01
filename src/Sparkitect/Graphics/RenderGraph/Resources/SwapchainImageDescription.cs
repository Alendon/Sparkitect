using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>
/// Shared engine description for a swapchain-backed present-target leaf. It declares only the plain
/// <see cref="ImageResource"/> backing and marks no moment — the finishline marking is the caller's
/// concern (a composite increments the finishline on the reference this sub-declaration returns).
/// </summary>
[PublicAPI]
public sealed record SwapchainImageDescription : IResourceDescription<ImageResource>
{
    /// <inheritdoc/>
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx) =>
        tx.InstantiateFact<SwapchainImageFact>();
}
