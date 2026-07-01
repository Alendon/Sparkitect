using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod.Resources;

/// <summary>
/// Mod-owned clear-color image description. It sub-declares the shared swapchain leaf, marks the engine
/// finishline moment on its own image increment (this image is the present target), and threads the leaf
/// reference into the fact that wraps it in a hook-contributing <see cref="ClearColorImageView"/>. The
/// finishline stays on this top-level chain; only the clear-color specifics are mod-owned.
/// </summary>
[PublicAPI]
public sealed record ClearColorImageDescription : IResourceDescription<ImageResource>
{
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx)
    {
        var leafRef = tx.Declare(new SwapchainImageDescription());
        tx.Increment(tx.Self<ImageResource>(), GraphMomentID.Sparkitect.Finishline);
        var fact = tx.InstantiateFact<ClearColorImageFact>();
        return fact with { Leaf = leafRef };
    }
}
