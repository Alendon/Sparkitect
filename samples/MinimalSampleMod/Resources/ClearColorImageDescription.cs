using JetBrains.Annotations;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod.Resources;

/// <summary>
/// Mod-owned clear-color image description. It marks the engine finishline moment on its own image
/// increment (this image is the present target) and instantiates the fact that resolves the swapchain
/// leaf into a hook-contributing <see cref="ClearColorImageView"/>. The declared chain type stays the
/// engine's plain <see cref="ImageResource"/>; only the clear-color specifics are mod-owned.
/// </summary>
[PublicAPI]
public sealed record ClearColorImageDescription : IResourceDescription<ImageResource>
{
    public DeclaredFact<ImageResource> Declare(IResourceTransaction tx)
    {
        tx.Increment(tx.Self<ImageResource>(), GraphMomentID.Sparkitect.Finishline);
        return tx.InstantiateFact<ClearColorImageFact>();
    }
}
