using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

/// <summary>
/// Per-graph <see cref="IResolutionProvider"/>. Currently a no-op pass-through — pass
/// constructors no longer take the window/swapchain directly; image access flows through
/// the resource layer. Reserved for future resource-shaped DI.
/// </summary>
internal sealed class RenderGraphResolutionProvider : IResolutionProvider
{
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        service = null;
        return false;
    }
}
