namespace Sparkitect.RenderGraph;

/// <summary>
/// Per-graph-scoped service exposing the current acquired swapchain image to passes.
/// </summary>
/// <remarks>
/// <para>
/// Honors D-E3 ("no global current-image service") by being scoped per <see cref="RenderGraph"/>
/// instance — there is no engine-wide singleton. Each <see cref="RenderGraph"/> owns a
/// <see cref="RenderGraphFrameContext"/> instance and exposes it to passes through its
/// <see cref="RenderGraphResolutionProvider"/> (the per-graph
/// <see cref="Sparkitect.DI.Resolution.IResolutionProvider"/> implementation passed to
/// <see cref="Sparkitect.DI.IDIService.BuildFactoryContainer{TKey,TBase}"/>).
/// </para>
/// <para>
/// The window-of-validity is the body of <see cref="IExecuteHook.Execute"/>: the
/// <see cref="RenderGraph.RunFrame"/> loop calls <see cref="RenderGraphFrameContext.SetCurrent"/>
/// just after <c>AcquireNextImage</c> and <see cref="RenderGraphFrameContext.ClearCurrent"/>
/// just after <c>Present</c>. Reads outside that window throw
/// <see cref="System.InvalidOperationException"/>.
/// </para>
/// <para>
/// This replaces the deprecated D-B5 global current-swapchain-image framing — the original
/// global-singleton shape is forbidden; the per-graph-scoped shape is the D-E3 answer.
/// </para>
/// </remarks>
public interface IRenderGraphFrameContext
{
    /// <summary>
    /// The image index returned by the most recent <c>AcquireNextImage</c> call on the
    /// owning <see cref="RenderGraph"/>'s swapchain.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown if read outside an Execute body (i.e. before the first <c>SetCurrent</c> call
    /// or after <c>ClearCurrent</c>).
    /// </exception>
    uint CurrentSwapchainImageIndex { get; }
}
