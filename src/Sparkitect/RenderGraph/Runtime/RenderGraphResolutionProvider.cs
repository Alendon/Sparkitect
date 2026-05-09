using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Windowing;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Per-graph custom <see cref="IResolutionProvider"/> (D-E3 step 3). Returns the per-graph
/// <see cref="ISparkitWindow"/>, <see cref="VkSwapchain"/>, and
/// <see cref="IRenderGraphFrameContext"/> instances when the requested
/// <paramref name="serviceType"/> matches one of those three; returns <c>false</c> for
/// everything else so <see cref="ResolutionScope"/> falls back to the host container per
/// <c>src/Sparkitect/DI/Resolution/ResolutionScope.cs:46-52</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Mechanism (D-E3 step 3 — load-bearing).</strong> Constructed by
/// <see cref="RenderGraph.Initialize"/> and passed as the <c>provider</c> argument of
/// <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>. Pass constructor parameters
/// of the three intercepted types resolve through this provider; everything else falls
/// through to the host <see cref="ICoreContainer"/> that <see cref="RenderGraph.Initialize"/>
/// passed unwrapped as the <c>container</c> argument.
/// </para>
/// <para>
/// <strong>NO container wrapping.</strong> Earlier iterations considered a per-graph
/// scoped <see cref="ICoreContainer"/> wrapper; D-E3 step 4 explicitly rejects
/// that mechanism. Using the documented <see cref="IResolutionProvider"/> extension point
/// avoids re-implementing the full <see cref="ICoreContainer"/> surface (including
/// <c>GetCurrentRegisteredInstances</c>) and stays inside the framework's intended seam —
/// proven by the canonical <see cref="FacadeResolutionProvider"/>.
/// </para>
/// </remarks>
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

    /// <inheritdoc />
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
