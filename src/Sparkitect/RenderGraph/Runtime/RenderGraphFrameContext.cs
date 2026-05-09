namespace Sparkitect.RenderGraph;

/// <summary>
/// Mutable internal implementation of <see cref="IRenderGraphFrameContext"/>. Updated only
/// from inside <see cref="RenderGraph.RunFrame"/> via <see cref="SetCurrent"/> and
/// <see cref="ClearCurrent"/>; passes see it through the read-only interface.
/// </summary>
internal sealed class RenderGraphFrameContext : IRenderGraphFrameContext
{
    private uint? _current;

    public uint CurrentSwapchainImageIndex =>
        _current ?? throw new InvalidOperationException(
            "IRenderGraphFrameContext.CurrentSwapchainImageIndex is only valid inside a pass Execute body.");

    internal void SetCurrent(uint index) => _current = index;

    internal void ClearCurrent() => _current = null;
}
