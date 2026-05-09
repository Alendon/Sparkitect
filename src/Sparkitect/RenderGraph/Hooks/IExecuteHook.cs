namespace Sparkitect.RenderGraph;

/// <summary>
/// Lifecycle hook invoked once per pass per frame (Execute phase).
/// </summary>
/// <remarks>
/// See <see cref="ISetupHook"/> for the broader hook-interface posture. The payload is
/// passed by <c>in</c> reference to avoid copying; per D-A2 / D-B4 / D-E2 the payload is a
/// minimal <c>readonly struct</c> exposing only the command buffer the pass body may record into.
/// </remarks>
public interface IExecuteHook
{
    void Execute(in ComputePassExecutePayload payload);
}
