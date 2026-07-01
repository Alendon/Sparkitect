using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Modding;

namespace PongMod.Resources;

/// <summary>
/// Builds the copy pass's read view: re-resolves the shared VMA-transient leaf from the image manager
/// (N=1 stable — the same instance the compute write view published) and wraps it, layout-only, in a
/// <see cref="TransferSrcReadView"/>. The <see cref="ExtentIntent"/> + <see cref="Format"/> flow in from
/// the description via a record <c>with</c> and must match the write view so the same N=1 leaf resolves.
/// </summary>
[FactRegistry.Register("pong_read_view")]
public sealed partial record ReadViewFact(IImageManager? Provider)
    : DeclaredFact<TransferSrcReadView>, IHasIdentification
{
    /// <summary>The symbolic size of the shared target, set by the description at Declare.</summary>
    public ExtentIntent Extent { get; init; } = new ExtentIntent.MatchSwapchain();

    /// <summary>The shared target's format, set by the description at Declare.</summary>
    public Format Format { get; init; } = Format.R8G8B8A8Unorm;

    /// <inheritdoc/>
    public TransferSrcReadView CreateInstance(IInstanceContext ctx)
    {
        if (Provider is null)
            throw new InvalidOperationException(
                "ReadViewFact.CreateInstance: no image backing provider was injected. The graph-local " +
                "IImageManager must be resolvable when the fact factory builds this fact.");

        var leaf = Provider.ResolveTransientLeaf(Extent, Format);
        return new TransferSrcReadView(leaf);
    }

    /// <summary>The view owns no GPU object; the shared transient leaf is manager-owned.</summary>
    public CleanupStrategy CleanupStrategy => CleanupStrategy.None;
}
