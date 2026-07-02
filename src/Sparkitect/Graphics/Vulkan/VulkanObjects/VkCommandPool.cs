using JetBrains.Annotations;
using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a command pool and the command buffers allocated from it. Destroying the pool invalidates all of them.</summary>
[PublicAPI]
public class VkCommandPool : VulkanObject
{
    private readonly HashSet<VkCommandBuffer> _allocatedBuffers = [];

    /// <summary>Wraps an existing <see cref="CommandPool"/> handle owned by <paramref name="vulkanContext"/>.</summary>
    public VkCommandPool(CommandPool pCommandPool, IVulkanContext vulkanContext, CallerContext callerContext = default)
        : base(vulkanContext, callerContext)
    {
        Handle = pCommandPool;
    }

    /// <summary>The underlying Silk.NET <see cref="CommandPool"/> handle.</summary>
    public CommandPool Handle { get; }

    /// <summary>Allocates a single command buffer of the given <paramref name="level"/> from this pool.</summary>
    public Result<VkCommandBuffer, VkApiResult> AllocateCommandBuffer(CommandBufferLevel level,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandBufferCount = 1,
            CommandPool = Handle,
            Level = level
        };

        var result = Vk.AllocateCommandBuffers(Device, in allocInfo, out var commandBuffer);

        if (result != VkApiResult.Success) return result;

        var buffer = new VkCommandBuffer(commandBuffer, VulkanContext, this, callerContext);
        _allocatedBuffers.Add(buffer);
        return buffer;
    }

    /// <summary>Allocates <paramref name="amount"/> command buffers of the given <paramref name="level"/> into a new array.</summary>
    public Result<VkCommandBuffer[], VkApiResult> AllocateCommandBuffers(CommandBufferLevel level, int amount,
        [InjectCallerContext] CallerContext callerContext = default)
    {
        VkCommandBuffer[] buffers = new VkCommandBuffer[amount];
        var result = AllocateCommandBuffers(level, buffers, callerContext);
        if (result != VkApiResult.Success) return result;

        return buffers;
    }

    /// <summary>Allocates command buffers of the given <paramref name="level"/> into the caller-provided <paramref name="target"/> span.</summary>
    public VkApiResult AllocateCommandBuffers(CommandBufferLevel level, Span<VkCommandBuffer> target,
        CallerContext callerContext = default)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandBufferCount = (uint)target.Length,
            CommandPool = Handle,
            Level = level
        };

        var buffers = target.Length < 1024
            ? stackalloc CommandBuffer[target.Length]
            : new CommandBuffer[target.Length];

        var result = Vk.AllocateCommandBuffers(Device, in allocInfo, out buffers[0]);

        if (result != VkApiResult.Success) return result;

        for (var i = 0; i < target.Length; i++)
        {
            var buffer = new VkCommandBuffer(buffers[i], VulkanContext, this, callerContext);
            target[i] = buffer;
            _allocatedBuffers.Add(buffer);
        }

        return result;
    }

    /// <summary>Resets the pool, recycling the memory of all command buffers allocated from it.</summary>
    public VkApiResult Reset(CommandPoolResetFlags flags)
    {
        return Vk.ResetCommandPool(Device, Handle, flags);
    }

    /// <summary>Frees the given command buffers back to the pool. Each buffer must have been allocated from this pool.</summary>
    public unsafe void FreeCommandBuffers(params ReadOnlySpan<VkCommandBuffer> buffers)
    {
        if (buffers.Length == 0) return;

        var handles = buffers.Length < 256
            ? stackalloc CommandBuffer[buffers.Length]
            : new CommandBuffer[buffers.Length];

        var count = 0;
        for (var i = 0; i < buffers.Length; i++)
        {
            var buffer = buffers[i];

            if (buffer.ParentPool != this)
                throw new ArgumentException($"Command buffer at index {i} was not allocated from this pool.", nameof(buffers));

            if (buffer.IsDisposed)
            {
                Log.Warning("Attempted to free already disposed command buffer at index {Index}", i);
                continue;
            }

            handles[count++] = buffer.Handle;
            _allocatedBuffers.Remove(buffer);
            buffer.MarkDisposed();
        }

        if (count == 0) return;

        fixed (CommandBuffer* ptr = handles)
        {
            Vk.FreeCommandBuffers(Device, Handle, (uint)count, ptr);
        }
    }

    /// <inheritdoc/>
    public override unsafe void Destroy()
    {
        foreach (var buffer in _allocatedBuffers)
        {
            buffer.MarkDisposed();
        }
        _allocatedBuffers.Clear();

        Vk.DestroyCommandPool(VulkanContext.VkDevice.Handle, Handle, AllocationCallbacks);
    }
}
