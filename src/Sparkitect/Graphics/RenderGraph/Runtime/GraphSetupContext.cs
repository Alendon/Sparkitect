using JetBrains.Annotations;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// The graph's <see cref="ISetupContext"/>. <see cref="Use{TResource}"/> declares a description into the
/// setup transaction and returns a handle bound to the per-frame instance context. Between
/// <see cref="BeginPass"/> and <see cref="EndPass"/> each use is also recorded as one of that pass's root
/// resources, so the frame loop can type-cast each root to the lifecycle hook interfaces.
/// </summary>
internal sealed class GraphSetupContext(
    ResourceTransaction transaction,
    FrameInstanceContext frameContext) : ISetupContext
{
    private List<RootResource>? _currentPassRoots;

    /// <inheritdoc/>
    public IGraphResource<TResource> Use<TResource>(IResourceDescription<TResource> description)
    {
        var reference = transaction.Declare(description);
        var handle = new GraphResourceHandle<TResource>(reference, frameContext);
        // Record this top-level use as a pass root. Sub-declarations run inside a description's Declare and
        // never reach this verb, so only genuine roots are captured.
        _currentPassRoots?.Add(new RootResource(reference.Resource, () => handle.Fetch()!));
        return handle;
    }

    /// <summary>Opens the recording window for one pass's Setup.</summary>
    public void BeginPass() => _currentPassRoots = [];

    /// <summary>Closes the pass's recording window and returns its ordered root resources.</summary>
    public IReadOnlyList<RootResource> EndPass()
    {
        var roots = (IReadOnlyList<RootResource>?)_currentPassRoots ?? [];
        _currentPassRoots = null;
        return roots;
    }
}

/// <summary>
/// One root resource captured during a pass's Setup: the resource chain's identity (used to correlate the
/// finishline-publishing root against the moment's marked increment) plus a fetch closure resolving the
/// live instance for the current frame.
/// </summary>
internal readonly record struct RootResource(GraphNodeId ResourceChain, Func<object> Fetch);
