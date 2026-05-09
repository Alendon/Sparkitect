using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.RenderGraph;

public sealed partial class RenderGraph
{
    /// <summary>
    /// Per-frame Vulkan orchestration at 1-frame-in-flight (D-C6, D-D9).
    /// Sequence: wait+reset fence → acquire → SetCurrent(frameContext) → begin cmd
    ///   → barrier(→TRANSFER_DST_OPTIMAL)
    ///   → for each pass: hand-dispatch IExecuteHook.Execute (D-D10)
    ///   → barrier(→PRESENT_SRC_KHR) → end cmd → submit → present → ClearCurrent.
    /// </summary>
    /// <remarks>
    /// Manually verified via MinimalSampleMod end-to-end run (per CONTEXT D-D2 +
    /// <c>feedback_no_smoke_test_plans</c> — no automated unit test for this method).
    /// </remarks>
    public unsafe void RunFrame()
    {
        var vk = _vulkanContext.VkApi;

        // 1. Wait for previous frame and reset fence (Pitfall 3).
        _inFlightFence.Wait();
        _inFlightFence.Reset();

        // 2. Acquire next swapchain image. autoRecreate:false — fail-fast on resize (Pitfall 4, D-D9).
        var acquireResult = _window.Swapchain.AcquireNextImage(_acquireSemaphore);
        if (acquireResult is not Result<uint, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException(
                "RenderGraph: AcquireNextImage failed (Phase 49 walking-skeleton has no resize support).");
        var imageIndex = acqOk.Value;

        // 3. Open the per-frame context window — passes can now read CurrentSwapchainImageIndex.
        _frameContext.SetCurrent(imageIndex);

        try
        {
            var swapchainImage = _window.Swapchain.Images[(int)imageIndex];

            // 4. Begin command buffer fresh.
            var cmd = _commandBuffer.Handle;
            vk.ResetCommandBuffer(cmd, 0);
            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };
            vk.BeginCommandBuffer(cmd, in beginInfo);

            // 5. Pre-pass barrier: UNDEFINED → TRANSFER_DST_OPTIMAL (D-D9).
            // UNDEFINED is permitted as oldLayout because the contents are about to be overwritten
            // by the clear (Pitfall 5).
            EmitImageMemoryBarrier(cmd, swapchainImage,
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.TransferDstOptimal,
                srcStage: PipelineStageFlags.TopOfPipeBit,
                dstStage: PipelineStageFlags.TransferBit,
                srcAccess: 0,
                dstAccess: AccessFlags.TransferWriteBit);

            // 6. Hand-dispatch each compiled pass (D-D10).
            var payload = new ComputePassExecutePayload(_commandBuffer);
            foreach (var (_, pass) in _compiled.OrderedPasses)
                ((IExecuteHook)pass).Execute(in payload);

            // 7. Post-pass barrier: TRANSFER_DST_OPTIMAL → PRESENT_SRC_KHR (D-D9).
            EmitImageMemoryBarrier(cmd, swapchainImage,
                oldLayout: ImageLayout.TransferDstOptimal,
                newLayout: ImageLayout.PresentSrcKhr,
                srcStage: PipelineStageFlags.TransferBit,
                dstStage: PipelineStageFlags.BottomOfPipeBit,
                srcAccess: AccessFlags.TransferWriteBit,
                dstAccess: 0);

            vk.EndCommandBuffer(cmd);

            // 8. Submit. Wait on acquire-semaphore at TRANSFER stage (the only writer is the clear).
            var waitSem = _acquireSemaphore.Handle;
            var signalSem = _presentSemaphore.Handle;
            var waitStage = PipelineStageFlags.TransferBit;
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &waitSem,
                PWaitDstStageMask = &waitStage,
                CommandBufferCount = 1,
                PCommandBuffers = &cmd,
                SignalSemaphoreCount = 1,
                PSignalSemaphores = &signalSem,
            };
            vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFence.Handle);

            // 9. Present.
            _ = _window.Swapchain.Present(imageIndex, _presentSemaphore, _graphicsQueue);
        }
        finally
        {
            // 10. Close the per-frame context window — reads outside Execute will throw.
            //     Wrapped in finally so a pass throwing during Execute does not leave a stale index.
            _frameContext.ClearCurrent();
        }
    }

    /// <summary>
    /// Records an image-memory barrier on <paramref name="cmd"/> using the engine's
    /// per-instance <see cref="Vk"/> dispatcher (<c>_vulkanContext.VkApi</c>) — same shape
    /// as <c>samples/PongMod/PongRuntimeService.cs:335</c>. The static Silk.NET dispatcher
    /// factory is forbidden here because it allocates a fresh dispatcher and breaks
    /// instance-dispatch consistency.
    /// </summary>
    private unsafe void EmitImageMemoryBarrier(
        CommandBuffer cmd,
        VkImage image,
        ImageLayout oldLayout, ImageLayout newLayout,
        PipelineStageFlags srcStage, PipelineStageFlags dstStage,
        AccessFlags srcAccess, AccessFlags dstAccess)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            Image = image.Handle,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0, LevelCount = 1,
                BaseArrayLayer = 0, LayerCount = 1,
            },
        };
        _vulkanContext.VkApi.CmdPipelineBarrier(
            cmd, srcStage, dstStage, 0,
            0, null, 0, null, 1, &barrier);
    }
}
