namespace Sparkitect.RenderGraph;

/// <summary>
/// Abstract base for compute-category render-graph passes. Authors implement
/// <see cref="Setup"/> and <see cref="Execute"/>; the base routes hook-interface
/// invocations through a slot-hook composition seam (default no-op; Phase 55 SG
/// emits an override per D-A6) before invoking the author method.
/// </summary>
/// <remarks>
/// <para>
/// Per D-A1 the user methods are <c>abstract</c> — both must be overridden.
/// Per D-A3 / D-A5 the base implements <see cref="ISetupHook"/> and <see cref="IExecuteHook"/>
/// explicitly so authors do not accidentally call them from outside; the only entry into
/// the user method goes through the hook composition seam.
/// </para>
/// <para>
/// Per D-A7 only the two hook interfaces ship at walking-skeleton; additional hooks
/// (PreExecute, PostExecute, Resize, Cleanup, FrameBinding) are added by their consumer phase.
/// </para>
/// </remarks>
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

    /// <summary>Author override — declare graph resource handles. Empty body is permitted (D-D5).</summary>
    public abstract void Setup();

    /// <summary>Author override — record pass-specific work into <paramref name="payload"/>.<see cref="ComputePassExecutePayload.CommandBuffer"/>.</summary>
    public abstract void Execute(in ComputePassExecutePayload payload);

    /// <summary>
    /// Slot-level Setup composition seam. Default is a no-op. Phase 55 SG emits a partial-class
    /// override that walks <c>[GraphResource]</c> slots and calls matching <see cref="ISetupHook"/>
    /// implementations on each slot view (per D-A6).
    /// </summary>
    protected virtual void InvokeSlotSetupHooks() { }

    /// <summary>
    /// Slot-level Execute composition seam. Default is a no-op. Phase 55 SG emits the override.
    /// </summary>
    protected virtual void InvokeSlotExecuteHooks(in ComputePassExecutePayload payload) { }
}
