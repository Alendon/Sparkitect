using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Graphics.RenderGraph.Resources;

/// <summary>Declares the swapchain write view: sub-declares the swapchain leaf, marks the engine finishline on it, and composes a hook-contributing <see cref="SwapchainWriteView"/> over it.</summary>
[PublicAPI]
public sealed record SwapchainWriteViewDescription : IResourceDescription<SwapchainWriteView>
{
    /// <inheritdoc/>
    public DeclaredFact<SwapchainWriteView> Declare(IResourceTransaction tx)
    {
        // The present target is a real image, so the finishline moment's increment node resolves to it directly.
        var backing = tx.Declare(new SwapchainImageDescription());
        tx.Increment(backing, GraphMomentID.Sparkitect.Finishline);

        var fact = tx.InstantiateFact<SwapchainWriteViewFact>();
        return fact with { Leaf = backing };
    }
}
