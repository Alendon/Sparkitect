using JetBrains.Annotations;
using Sparkitect.Graphing;
using Sparkitect.Graphing.Descriptions;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// The graph's <see cref="ISetupContext"/>: the single <see cref="Use{TResource}"/> verb declares a
/// description into the setup transaction and returns a handle bound to the rebindable per-frame
/// instance context. While a pass is being set up (between <see cref="BeginPass"/> and
/// <see cref="EndPass"/>) each <see cref="Use{TResource}"/> is also recorded as one of that pass's
/// plan-derived ROOT resources, so the frame loop can type-cast each root instance to the lifecycle
/// hook interfaces and dispatch synchronization without coupling to pass-private fields. A root
/// resource is itself responsible for cascading to any sub-resources it owns.
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
        // Record this top-level use as a pass root (D-07a discovery source: plan-derived root
        // resources, not pass-private fields). Sub-declarations run inside a description's Declare and
        // never reach this verb, so only genuine roots are captured. The fetch closure resolves the
        // instance against whatever per-frame context is bound at dispatch time.
        _currentPassRoots?.Add(new RootResource(reference.Resource, () => handle.Fetch()!));
        return handle;
    }

    /// <summary>Opens the recording window for one pass's Setup; roots used until <see cref="EndPass"/> belong to it.</summary>
    public void BeginPass() => _currentPassRoots = [];

    /// <summary>Closes the current pass's recording window and returns its ordered root resources.</summary>
    public IReadOnlyList<RootResource> EndPass()
    {
        var roots = (IReadOnlyList<RootResource>?)_currentPassRoots ?? [];
        _currentPassRoots = null;
        return roots;
    }
}

/// <summary>
/// One plan-derived root resource captured during a pass's Setup: the identity of the resource chain it
/// declared (used to correlate the finishline-publishing root against the finishline moment's marked
/// increment) plus a fetch closure resolving the live instance for the current frame (the target of the
/// runtime lifecycle-hook type-cast).
/// </summary>
internal readonly record struct RootResource(GraphNodeId ResourceChain, Func<object> Fetch);
