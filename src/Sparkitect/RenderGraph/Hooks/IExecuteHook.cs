namespace Sparkitect.RenderGraph;

/// <summary>
/// Lifecycle hook invoked once per pass per frame.
/// </summary>
public interface IExecuteHook
{
    void Execute(in ComputePassExecutePayload payload);
}
