using System.Diagnostics;
using Silk.NET.Vulkan;
using Sparkitect.CompilerGenerated;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphics.RenderGraph.Push;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

public sealed partial class RenderGraph
{
    /// <summary>
    /// Per-frame Vulkan orchestration at 1-frame-in-flight: waits, acquires, binds a fresh instance
    /// context, dispatches each pass's pre-execute hooks then its Execute, dispatches the finishline hook,
    /// asserts the present target is <see cref="ImageLayout.PresentSrcKhr"/>, then submits and presents.
    /// The graph issues NO barriers itself; sync is entirely hook-contributed.
    /// </summary>
    public void RunFrame()
    {
        if (!_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.RunFrame: Setup has not been invoked. Construct render graphs via " +
                "IRenderGraphManager.CreateGraph<TRenderGraph>(passIds, window).");

        // FPS cap read inline from settings each frame (D-17); 0 = uncapped (preserved semantics).
        var fpsCap = _settingsManager.Graphics.FpsCap.Value;
        if (fpsCap != 0)
        {
            var minFrameTimeS = 1d / fpsCap;
            SpinWait.SpinUntil(() =>
                Stopwatch.GetElapsedTime(_lastFrameTimestamp, Stopwatch.GetTimestamp()).TotalSeconds > minFrameTimeS);
            _lastFrameTimestamp = Stopwatch.GetTimestamp();
        }

        _inFlightFence.Wait();
        _inFlightFence.Reset();

        var acquireResult = _window.Swapchain.AcquireNextImage(_acquireSemaphore);
        if (acquireResult is not Result<uint, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException(
                "RenderGraph: AcquireNextImage failed (resize is not supported).");
        var imageIndex = acqOk.Value;

        // Bind a FRESH per-frame instance context so the leaf resolves against the current acquired index.
        ImageManager.InformAcquiredIndex(imageIndex);
        var instanceContext = new InstanceContext(_transaction, _plan.ResolvedMoments, _ledger);
        _frameContext.Bind(instanceContext);

        // Frame-start drain: surface (and clear) any validation ERROR captured outside a frame boundary
        // before this frame's work begins — between-frames hygiene for the pending-error slot.
        _vulkanContext.ThrowIfPendingValidationError();

        // Frame-start external push: bind each registered pushed moment's latest snapshot to its
        // chain-head instance before any pass runs. The rebind is unconditional — when nothing new was
        // published this frame the store returns the previous snapshot, keeping the chain head bound. L2
        // only binds the snapshot here; the birth increment is the L1 chain-head epoch (synthesized at Setup).
        foreach (var pushedMoment in _pushedMoments)
        {
            var pushed = instanceContext.ResolveMoment<PushedResource>(pushedMoment);
            pushed.Bind(_pushStore.Latest(pushedMoment));
        }

        // One-time validation: the present target must resolve to a swapchain-backed image. Deferred to
        // the first frame because Fetch requires a bound instance context (unavailable during Setup).
        if (!_presentBackingValidated)
        {
            ValidatePresentTargetIsSwapchainBacked();
            _presentBackingValidated = true;
        }

        _commandBuffer.Reset();
        _commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        // Before each pass Executes, dispatch the pre-execute hook on each of its root resources by
        // type-casting the resolved instance to the hook interface.
        for (var i = 0; i < _passes.Count; i++)
        {
            foreach (var root in _passRoots[i])
                if (root.Fetch() is IPreExecuteHook preHook)
                    preHook.PreExecute(_commandBuffer);

            _passes[i].Execute(_commandBuffer);

            // Per-pass chokepoint: a recording defect throws here at pass granularity with the VUID text.
            _vulkanContext.ThrowIfPendingValidationError();
        }

        // After ALL passes, dispatch the finishline hook on the publishing root: the present transition
        // must fire after the final pass (e.g. after a blit) and is contributed by the resource, not the RG.
        if (_finishlinePublisher is { } publisher && publisher.Fetch() is IFinishlineHook finishlineHook)
            finishlineHook.OnFinishline(_commandBuffer);

        // Present-readiness assert: the RG issues no present transition — it fails fast if a hook left the target non-presentable.
        var presentLeaf = _presentTarget!.Fetch();
        if (presentLeaf.CurrentLayout != ImageLayout.PresentSrcKhr)
            throw new InvalidOperationException(
                $"RenderGraph.RunFrame: the present target is in {presentLeaf.CurrentLayout}, not " +
                $"{ImageLayout.PresentSrcKhr}. The finishline-publishing resource's lifecycle hook must " +
                "transition it to the present layout; the render graph issues no present transition itself.");

        _commandBuffer.End();

        var presentSemaphore = _presentSemaphores[imageIndex];
        _graphicsQueue.Submit(
            _commandBuffer,
            waitSemaphores: [_acquireSemaphore],
            waitStages: [PipelineStageFlags.TransferBit],
            signalSemaphores: [presentSemaphore],
            fence: _inFlightFence);

        // Submit chokepoint: a submit-time validation defect throws before present.
        _vulkanContext.ThrowIfPendingValidationError();

        _ = _window.Swapchain.Present(imageIndex, presentSemaphore, _graphicsQueue);

        // Present chokepoint: a present-time validation defect throws rather than being dropped.
        _vulkanContext.ThrowIfPendingValidationError();
    }

    // Validates once that the present target resolves to one of the swapchain's images; a non-swapchain
    // backing is a wiring error surfaced before any present is attempted.
    private void ValidatePresentTargetIsSwapchainBacked()
    {
        var presentBacking = _presentTarget!.Fetch().Backing;
        foreach (var image in _window.Swapchain.Images)
            if (ReferenceEquals(image, presentBacking))
                return;

        throw new InvalidOperationException(
            "RenderGraph: the finishline present target does not resolve to a swapchain-backed image. " +
            "The finishline moment must be published by a resource whose backing is one of the swapchain " +
            "images so the graph can present it.");
    }
}
