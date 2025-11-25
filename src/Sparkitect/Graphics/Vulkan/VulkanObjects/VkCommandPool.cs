using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkCommandPool : VulkanObject
{
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
        
        return VkResult<VkCommandBuffer>._Success(new VkCommandBuffer(commandBuffer, VulkanContext, this));
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
            target[i] = new VkCommandBuffer(buffers[i], VulkanContext, this);
        }

        return result;
    }
    
    
    
    
    public override unsafe void Destroy()
    {
        Vk.DestroyCommandPool(VulkanContext.VkDevice.Handle, Handle, AllocationCallbacks);
    }
}