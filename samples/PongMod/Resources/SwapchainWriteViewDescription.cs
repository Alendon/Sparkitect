using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the copy pass's swapchain write view. It marks the engine finishline moment on its own
/// increment — this image is the present target the copy blits into and presents (Pong marks the finishline
/// AFTER the copy, declared in PongMod first and promoted later per D-03) — then instantiates the fact that
/// resolves the swapchain leaf into a hook-contributing <see cref="SwapchainWriteView"/>.
/// </summary>
[PublicAPI]
public sealed record SwapchainWriteViewDescription : IResourceDescription<SwapchainWriteView>
{
    /// <inheritdoc/>
    public DeclaredFact<SwapchainWriteView> Declare(IResourceTransaction tx)
    {
        // Mark the finishline on this view's own increment: it is the present target, and the present
        // transition it carries as a finishline hook fires after every pass (D-03/D-09).
        tx.Increment(tx.Self<SwapchainWriteView>(), GraphMomentID.Sparkitect.Finishline);
        return tx.InstantiateFact<SwapchainWriteViewFact>();
    }
}
