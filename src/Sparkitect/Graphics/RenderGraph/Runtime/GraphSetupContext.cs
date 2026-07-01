using JetBrains.Annotations;
using Sparkitect.Graphics.RenderGraph.Resources;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// The graph's <see cref="ISetupContext"/>: the single <see cref="Use{TResource}"/> verb threads the
/// graph-owned image backing provider into any description exposing that seam, declares the description
/// into the setup transaction, and returns a handle bound to the rebindable per-frame instance context.
/// A description that resolves a present target (the finishline-marked clear image) is captured so the
/// frame loop can re-fetch that exact carried-state leaf for the finishline present transition.
/// </summary>
internal sealed class GraphSetupContext(
    ResourceTransaction transaction,
    FrameInstanceContext frameContext) : ISetupContext
{

    /// <inheritdoc/>
    public IGraphResource<TResource> Use<TResource>(IResourceDescription<TResource> description)
    {
        var reference = transaction.Declare(description);
        return new GraphResourceHandle<TResource>(reference, frameContext);
    }
}
