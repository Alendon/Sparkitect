using System.Diagnostics;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph.Hooks;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

public sealed partial class RenderGraph
{
    /// <summary>
    /// Hand-wired per-frame Vulkan orchestration at 1-frame-in-flight. Waits for the previous frame,
    /// acquires the next swapchain image, informs the backing provider of the acquired index, binds a
    /// fresh per-frame instance context, then for each pass dispatches its plan-derived root resources'
    /// pre-execute lifecycle hooks (type-cast to <see cref="IPreExecuteHook"/>) before the pass executes.
    /// After all passes it dispatches the finishline (present) hook on the finishline-publishing root
    /// (<see cref="IFinishlineHook"/>) and then asserts the present target's carried state is
    /// <see cref="ImageLayout.PresentSrcKhr"/> — the graph issues NO barriers or transitions itself; sync
    /// is entirely hook-contributed. Finally it submits and presents.
    /// </summary>
    public void RunFrame()
    {
        if (!_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.RunFrame: Setup has not been invoked. Construct render graphs via " +
                "IRenderGraphManager.CreateGraph<TRenderGraph>(passIds, window).");

        if (MaxFrameRate != 0)
        {
            var minFrameTimeS = 1d / MaxFrameRate;
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

        // Inform the backing provider of this frame's acquired index, then bind a FRESH per-frame
        // instance context so the leaf resolves against the current index (Pitfall 3: N=1 cache).
        ImageManager.InformAcquiredIndex(imageIndex);
        var instanceContext = new InstanceContext(_transaction);
        _frameContext.Bind(instanceContext);

        // One-time, post-setup validation (D-03): the finishline present target must resolve to a
        // swapchain-backed image. Deferred to the first frame because Fetch requires a bound instance
        // context (unavailable during Setup); the resolution is build-once and never changes after.
        if (!_presentBackingValidated)
        {
            ValidatePresentTargetIsSwapchainBacked();
            _presentBackingValidated = true;
        }

        _commandBuffer.Reset();
        _commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        // Run the passes. Before each pass Executes, dispatch the pre-execute lifecycle hook on each of
        // that pass's plan-derived root resources by type-casting the resolved instance to the hook
        // interface (D-07/D-07a) — replacing the deprecated pass-invoked PreExecute calls. A root's hook
        // body reconciles its own carried state and may cascade to any sub-resources it owns.
        for (var i = 0; i < _passes.Count; i++)
        {
            foreach (var root in _passRoots[i])
                if (root.Fetch() is IPreExecuteHook preHook)
                    preHook.PreExecute(_commandBuffer);

            _passes[i].Execute(_commandBuffer);
        }

        // Finishline position: after ALL passes, dispatch the finishline hook on the finishline-publishing
        // root (Pitfall 2 — the present transition must fire after the final pass, e.g. after a blit). The
        // present-layout transition is contributed here by the resource, never issued by the RG (D-09).
        if (_finishlinePublisher is { } publisher && publisher.Fetch() is IFinishlineHook finishlineHook)
            finishlineHook.OnFinishline(_commandBuffer);

        // Present-readiness assert (D-09): the RG issues NO present transition — it reads the present
        // target's carried state and fails fast with named context if a hook left it non-presentable.
        var presentLeaf = _presentTarget!.Fetch();
        if (presentLeaf.CurrentLayout != ImageLayout.PresentSrcKhr)
            throw new InvalidOperationException(
                $"RenderGraph.RunFrame: the present target is in {presentLeaf.CurrentLayout}, not " +
                $"{ImageLayout.PresentSrcKhr}. The finishline-publishing resource's lifecycle hook must " +
                "transition it to the present layout; the render graph issues no present transition itself.");

        _commandBuffer.End();

        _graphicsQueue.Submit(
            _commandBuffer,
            waitSemaphores: [_acquireSemaphore],
            waitStages: [PipelineStageFlags.TransferBit],
            signalSemaphores: [_presentSemaphore],
            fence: _inFlightFence);

        _ = _window.Swapchain.Present(imageIndex, _presentSemaphore, _graphicsQueue);
    }

    // Validates once that the finishline present target resolves to one of the swapchain's images. The
    // finishline moment names the presentable swapchain output (D-09); a non-swapchain backing here is a
    // wiring error surfaced with named context before any present is attempted.
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
