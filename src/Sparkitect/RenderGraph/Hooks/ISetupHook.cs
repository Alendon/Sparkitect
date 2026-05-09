namespace Sparkitect.RenderGraph;

/// <summary>
/// Lifecycle hook invoked once per pass during graph initialization (Setup phase).
/// </summary>
/// <remarks>
/// Per Phase 49 D-A3 / D-A7, this is one of exactly two hook interfaces shipping at
/// walking-skeleton scope. <see cref="ComputePass"/> implements it explicitly and routes
/// to its abstract <c>Setup()</c> method through a slot-hook composition seam (D-A5/D-A6).
/// Additional hook interfaces (PreExecute, PostExecute, Resize, Cleanup, FrameBinding) ship
/// when a consumer phase needs them — not in Phase 49.
/// </remarks>
public interface ISetupHook
{
    void Setup();
}
