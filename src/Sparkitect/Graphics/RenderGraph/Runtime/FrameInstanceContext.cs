using JetBrains.Annotations;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Graphing.Ledger;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// A rebindable indirection over the per-frame <see cref="InstanceContext"/>. Pass handles capture this
/// proxy once at setup (via <see cref="GraphSetupContext.Use{TResource}"/>); every frame the graph swaps
/// in a fresh <see cref="InstanceContext"/> over the setup transaction so a handle's <c>Fetch</c>
/// resolves against the currently-acquired index (N=1, the index-changes-each-frame contract). Without
/// this indirection a handle would pin the setup-time context and cache a stale single-index leaf.
/// </summary>
[PublicAPI]
public sealed class FrameInstanceContext : IInstanceContext
{
    private IInstanceContext? _current;

    /// <summary>Binds the instance context resolution flows through for the current frame.</summary>
    public void Bind(IInstanceContext current) => _current = current;

    /// <inheritdoc/>
    public T Resolve<T>(ResourceRef<T> reference)
    {
        if (_current is null)
            throw new InvalidOperationException(
                "FrameInstanceContext.Resolve: no per-frame instance context is bound. Fetch is only " +
                "valid during Execute, after the frame loop binds a fresh InstanceContext.");

        return _current.Resolve(reference);
    }
}
