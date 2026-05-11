namespace Sparkitect.RenderGraph;

/// <summary>
/// Per-graph-scoped service exposing the current acquired swapchain image to passes.
/// Each <see cref="RenderGraph"/> owns its own instance; there is no global singleton.
/// </summary>
public interface IRenderGraphFrameContext
{
    /// <summary>
    /// The image index from the most recent <c>AcquireNextImage</c> call on the owning
    /// <see cref="RenderGraph"/>'s swapchain. Valid only inside <see cref="IExecuteHook.Execute"/>;
    /// reads outside that window throw <see cref="System.InvalidOperationException"/>.
    /// </summary>
    uint CurrentSwapchainImageIndex { get; }
}
