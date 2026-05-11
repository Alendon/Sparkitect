using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.RenderGraph;

public sealed partial class RenderGraph
{
    /// <summary>
    /// Per-frame Vulkan orchestration at 1-frame-in-flight. Waits for the previous frame,
    /// acquires the next swapchain image, dispatches every compiled pass between layout
    /// barriers, then submits and presents.
    /// </summary>
    public unsafe void RunFrame()
    {
        var vk = _vulkanContext.VkApi;

        _inFlightFence.Wait();
        _inFlightFence.Reset();

        // autoRecreate:false — resize is not supported yet, fail-fast.
        var acquireResult = _window.Swapchain.AcquireNextImage(_acquireSemaphore);
        if (acquireResult is not Result<uint, VkApiResult>.Ok acqOk)
            throw new InvalidOperationException(
                "RenderGraph: AcquireNextImage failed (resize is not supported).");
        var imageIndex = acqOk.Value;

        _frameContext.SetCurrent(imageIndex);

        try
        {
            var swapchainImage = _window.Swapchain.Images[(int)imageIndex];

            var cmd = _commandBuffer.Handle;
            vk.ResetCommandBuffer(cmd, 0);
            var beginInfo = new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmitBit
            };
            vk.BeginCommandBuffer(cmd, in beginInfo);

            // UNDEFINED is permitted as oldLayout because the contents are about to be overwritten.
            EmitImageMemoryBarrier(cmd, swapchainImage,
                oldLayout: ImageLayout.Undefined,
                newLayout: ImageLayout.TransferDstOptimal,
                srcStage: PipelineStageFlags.TopOfPipeBit,
                dstStage: PipelineStageFlags.TransferBit,
                srcAccess: 0,
                dstAccess: AccessFlags.TransferWriteBit);

            var payload = new ComputePassExecutePayload(_commandBuffer);
            foreach (var (_, pass) in _compiled.OrderedPasses)
                ((IExecuteHook)pass).Execute(in payload);

            EmitImageMemoryBarrier(cmd, swapchainImage,
                oldLayout: ImageLayout.TransferDstOptimal,
                newLayout: ImageLayout.PresentSrcKhr,
                srcStage: PipelineStageFlags.TransferBit,
                dstStage: PipelineStageFlags.BottomOfPipeBit,
                srcAccess: AccessFlags.TransferWriteBit,
                dstAccess: 0);

            vk.EndCommandBuffer(cmd);

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

            _ = _window.Swapchain.Present(imageIndex, _presentSemaphore, _graphicsQueue);
        }
        finally
        {
            // Wrapped in finally so a pass throwing during Execute does not leave a stale index.
            _frameContext.ClearCurrent();
        }
    }

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
