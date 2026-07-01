using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace PongMod.Resources;

/// <summary>
/// Declaration of the copy pass's swapchain write view. It sub-declares the shared swapchain-backed image
/// leaf and marks the engine finishline moment on that plain <see cref="ImageResource"/> reference — the
/// present target the copy blits into and presents — then instantiates the fact that composes a
/// hook-contributing <see cref="SwapchainWriteView"/> over that same leaf.
/// </summary>
[PublicAPI]
public sealed record SwapchainWriteViewDescription : IResourceDescription<SwapchainWriteView>
{
    /// <inheritdoc/>
    public DeclaredFact<SwapchainWriteView> Declare(IResourceTransaction tx)
    {
        // Sub-declare the shared swapchain leaf and mark the finishline on that plain ImageResource ref:
        // the present target is a real image, so the moment's increment node resolves to it directly and
        // the present transition the composite contributes as a finishline hook rides the same leaf.
        var backing = tx.Declare(new SwapchainImageDescription());
        tx.Increment(backing, GraphMomentID.Sparkitect.Finishline);

        var fact = tx.InstantiateFact<SwapchainWriteViewFact>();
        return fact with { Leaf = backing };
    }
}
