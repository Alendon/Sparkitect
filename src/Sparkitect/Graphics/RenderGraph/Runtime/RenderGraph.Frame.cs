using System.Diagnostics;
using Silk.NET.Vulkan;
using Sparkitect.Graphing.Descriptions;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph.Runtime;

public sealed partial class RenderGraph
{
    /// <summary>
    /// Hand-wired per-frame Vulkan orchestration at 1-frame-in-flight. Waits for the previous frame,
    /// acquires the next swapchain image, informs the backing provider of the acquired index, binds a
    /// fresh per-frame instance context, runs the compiled passes (each resolving its leaf lazily at
    /// <c>Fetch</c> and doing its own imperative transition), issues the finishline present-layout
    /// transition reconciling the leaf's carried state to <see cref="ImageLayout.PresentSrcKhr"/>, then
    /// submits and presents. No plan-emitted barriers — every transition resolves at runtime.
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
        _provider.InformAcquiredIndex(imageIndex);
        var instanceContext = new InstanceContext(_transaction);
        _frameContext.Bind(instanceContext);

        _commandBuffer.Reset();
        _commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        // Run the passes in compiled-plan order. For the walking skeleton there is exactly one pass;
        // each pass Fetches its leaf (resolving against this frame's index) and does its one imperative
        // pre-transition + clear, writing the resulting layout into the leaf's carried state.
        foreach (var pass in _passes)
            pass.Execute(_commandBuffer);

        // Finishline present transition: after the ordered passes run, resolve the finishline-marked
        // leaf (the same cached instance the pass mutated) and reconcile its CARRIED state to the
        // present layout. The finishline is the last increment for the one-pass skeleton.
        var presentLeaf = _presentTarget!.Fetch();
        presentLeaf.TransitionTo(
            _commandBuffer,
            ImageLayout.PresentSrcKhr,
            newAccess: AccessFlags.MemoryReadBit,
            dstStage: PipelineStageFlags.BottomOfPipeBit);

        _commandBuffer.End();

        _graphicsQueue.Submit(
            _commandBuffer,
            waitSemaphores: [_acquireSemaphore],
            waitStages: [PipelineStageFlags.TransferBit],
            signalSemaphores: [_presentSemaphore],
            fence: _inFlightFence);

        _ = _window.Swapchain.Present(imageIndex, _presentSemaphore, _graphicsQueue);
    }
}
