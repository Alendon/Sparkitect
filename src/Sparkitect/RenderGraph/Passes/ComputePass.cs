namespace Sparkitect.RenderGraph;

/// <summary>
/// Abstract base for compute-category render-graph passes. Authors implement
/// <see cref="Setup"/> and <see cref="Execute"/>; the base routes hook-interface
/// invocations through a slot composition seam before invoking the author method.
/// </summary>
public abstract class ComputePass : IPass, ISetupHook, IExecuteHook
{
    void ISetupHook.Setup()
    {
        InvokeSlotSetupHooks();
        Setup();
    }

    void IExecuteHook.Execute(in ComputePassExecutePayload payload)
    {
        InvokeSlotExecuteHooks(in payload);
        Execute(in payload);
    }

    /// <summary>Author override — declare graph resource handles. Empty body is permitted.</summary>
    public abstract void Setup();

    /// <summary>Author override — record pass-specific work into <paramref name="payload"/>'s command buffer.</summary>
    public abstract void Execute(in ComputePassExecutePayload payload);

    /// <summary>Slot-level Setup composition seam. Default no-op; later generators emit a partial override.</summary>
    protected virtual void InvokeSlotSetupHooks() { }

    /// <summary>Slot-level Execute composition seam. Default no-op; later generators emit a partial override.</summary>
    protected virtual void InvokeSlotExecuteHooks(in ComputePassExecutePayload payload) { }
}
