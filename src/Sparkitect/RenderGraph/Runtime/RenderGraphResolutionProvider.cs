using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Windowing;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Per-graph <see cref="IResolutionProvider"/> that supplies the graph's
/// <see cref="ISparkitWindow"/>, <see cref="VkSwapchain"/>, and
/// <see cref="IRenderGraphFrameContext"/> to pass constructors. Returns <c>false</c>
/// for any other type so resolution falls through to the host container.
/// </summary>
internal sealed class RenderGraphResolutionProvider : IResolutionProvider
{
    private readonly ISparkitWindow _window;
    private readonly VkSwapchain _swapchain;
    private readonly IRenderGraphFrameContext _frameContext;

    public RenderGraphResolutionProvider(
        ISparkitWindow window,
        VkSwapchain swapchain,
        IRenderGraphFrameContext frameContext)
    {
        _window = window;
        _swapchain = swapchain;
        _frameContext = frameContext;
    }

    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        if (serviceType == typeof(ISparkitWindow))
        {
            service = _window;
            return true;
        }
        if (serviceType == typeof(VkSwapchain))
        {
            service = _swapchain;
            return true;
        }
        if (serviceType == typeof(IRenderGraphFrameContext))
        {
            service = _frameContext;
            return true;
        }

        service = null;
        return false;
    }
}
