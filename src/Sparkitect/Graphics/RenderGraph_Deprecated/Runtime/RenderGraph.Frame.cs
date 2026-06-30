using System.Diagnostics;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated.Hooks;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Runtime;

public sealed partial class RenderGraphDeprecated
{
    /// <summary>
    /// Per-frame Vulkan orchestration at 1-frame-in-flight. Waits for the previous frame,
    /// acquires the next swapchain image, dispatches every compiled pass between
    /// manager-driven barriers, then submits and presents.
    /// </summary>
    public void RunFrame()
    {
        if (!_setupComplete)
            throw new InvalidOperationException(
                "RenderGraph.RunFrame: Setup has not been invoked. Construct render graphs via IRenderGraphManager.CreateGraph<TRenderGraph>(passIds).");

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

        _imageManager.BeginFrame(imageIndex);

        _commandBuffer.Reset();
        _commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmitBit);

        _imageManager.ApplyPendingFills(_commandBuffer);

        foreach (var (_, pass) in _compiled.OrderedPasses)
            ((IExecuteHook)pass).Execute(_commandBuffer);

        _imageManager.EndFrame(_commandBuffer);

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
