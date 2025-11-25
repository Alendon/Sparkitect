using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkCommandPool : VulkanObject
{
    private readonly HashSet<VkCommandBuffer> _allocatedBuffers = [];

    public VkCommandPool(CommandPool pCommandPool, IVulkanContext vulkanContext) : base(vulkanContext)
    {
        Handle = pCommandPool;
    }

    public CommandPool Handle { get; }
    

    public VkResult<VkCommandBuffer> AllocateCommandBuffer(CommandBufferLevel level)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandBufferCount = 1,
            CommandPool = Handle,
            Level = level
        };

        var result = Vk.AllocateCommandBuffers(Device, allocInfo, out var commandBuffer);
        
        if(result != Result.Success) return VkResult<VkCommandBuffer>._Error(result);

        var buffer = new VkCommandBuffer(commandBuffer, VulkanContext, this);
        _allocatedBuffers.Add(buffer);
        return VkResult<VkCommandBuffer>._Success(buffer);
    }

    public VkResult<VkCommandBuffer[]> AllocateCommandBuffers(CommandBufferLevel level, int amount)
    {
        VkCommandBuffer[] buffers = new VkCommandBuffer[amount];
        var result = AllocateCommandBuffers(level, buffers);
        if(result != Result.Success) return VkResult<VkCommandBuffer[]>._Error(result);

        return VkResult<VkCommandBuffer[]>._Success(buffers);
    }
    
    public Result AllocateCommandBuffers(CommandBufferLevel level, Span<VkCommandBuffer> target)
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
        
        var result = Vk.AllocateCommandBuffers(Device, allocInfo, out buffers[0]);

        if (result != Result.Success) return result;

        for (var i = 0; i < target.Length; i++)
        {
            var buffer = new VkCommandBuffer(buffers[i], VulkanContext, this);
            target[i] = buffer;
            _allocatedBuffers.Add(buffer);
        }

        return result;
    }

    public Result Reset(CommandPoolResetFlags flags)
    {
        return Vk.ResetCommandPool(Device, Handle, flags);
    }

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