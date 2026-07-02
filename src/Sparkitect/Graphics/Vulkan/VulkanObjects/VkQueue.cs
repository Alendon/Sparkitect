using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Utils;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>
/// Wrapper for a Vulkan queue retrieved from a device. Queues are not destroyed
/// individually; <see cref="Destroy"/> is a no-op. The wrapper participates in
/// <see cref="VulkanObject"/> for tracker and <see cref="IVulkanContext"/> consistency.
/// </summary>
[PublicAPI]
public sealed class VkQueue : VulkanObject
{
    /// <summary>Wraps a queue retrieved from the device, recording its family, index, and capability flags.</summary>
    public VkQueue(
        Queue handle,
        uint familyIndex,
        uint queueIndex,
        QueueFlags capabilities,
        IVulkanContext vulkanContext,
        CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = handle;
        FamilyIndex = familyIndex;
        QueueIndex = queueIndex;
        Capabilities = capabilities;
    }

    /// <summary>
    /// The native Vulkan queue handle.
    /// </summary>
    public Queue Handle { get; }

    /// <summary>
    /// The queue family this queue belongs to.
    /// </summary>
    public uint FamilyIndex { get; }

    /// <summary>
    /// The index of this queue within its family.
    /// </summary>
    public uint QueueIndex { get; }

    /// <summary>
    /// The capability flags of this queue's family.
    /// </summary>
    public QueueFlags Capabilities { get; }

    /// <summary>
    /// Submits a single command buffer with wait/signal semaphores and a fence.
    /// Wait-stage count must equal wait-semaphore count.
    /// </summary>
    public unsafe VkApiResult Submit(
        VkCommandBuffer cmd,
        ReadOnlySpan<VkSemaphore> waitSemaphores,
        ReadOnlySpan<PipelineStageFlags> waitStages,
        ReadOnlySpan<VkSemaphore> signalSemaphores,
        VkFence fence)
    {
        if (waitSemaphores.Length != waitStages.Length)
            throw new ArgumentException(
                $"waitSemaphores.Length ({waitSemaphores.Length}) must equal waitStages.Length ({waitStages.Length})");

        // Materialize handle arrays for the two semaphore sides. The raw stages
        // span is pinned directly. For zero-length spans the pinned pointer is
        // null, which is legal when the matching count field is 0 per the
        // Vulkan spec (VkSubmitInfo wait/signal arrays are ignored when their
        // count is 0).
        var waitHandles = waitSemaphores.Length <= 16
            ? stackalloc Semaphore[waitSemaphores.Length]
            : new Semaphore[waitSemaphores.Length];
        for (var i = 0; i < waitSemaphores.Length; i++)
            waitHandles[i] = waitSemaphores[i].Handle;

        var signalHandles = signalSemaphores.Length <= 16
            ? stackalloc Semaphore[signalSemaphores.Length]
            : new Semaphore[signalSemaphores.Length];
        for (var i = 0; i < signalSemaphores.Length; i++)
            signalHandles[i] = signalSemaphores[i].Handle;

        var cmdHandle = cmd.Handle;

        fixed (Semaphore* waitPtr = waitHandles)
        fixed (PipelineStageFlags* stagePtr = waitStages)
        fixed (Semaphore* signalPtr = signalHandles)
        {
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = (uint)waitSemaphores.Length,
                PWaitSemaphores = waitPtr,
                PWaitDstStageMask = stagePtr,
                CommandBufferCount = 1,
                PCommandBuffers = &cmdHandle,
                SignalSemaphoreCount = (uint)signalSemaphores.Length,
                PSignalSemaphores = signalPtr,
            };
            return Vk.QueueSubmit(Handle, 1, in submitInfo, fence.Handle);
        }
    }

    /// <summary>
    /// Convenience overload: submit a single command buffer with no semaphores
    /// and no fence. Equivalent to passing <c>default(Fence)</c> as the fence
    /// handle to <c>vkQueueSubmit</c>, which the Vulkan spec accepts as
    /// "no fence".
    /// </summary>
    public unsafe VkApiResult Submit(VkCommandBuffer cmd)
    {
        var cmdHandle = cmd.Handle;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdHandle,
        };
        return Vk.QueueSubmit(Handle, 1, in submitInfo, default);
    }

    /// <summary>No-op: queues are owned by the device and never destroyed individually.</summary>
    public override void Destroy()
    {
    }
}
