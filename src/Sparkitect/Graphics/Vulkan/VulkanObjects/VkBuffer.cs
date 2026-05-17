using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

[PublicAPI]
public class VkBuffer : VulkanObject
{
    private readonly VmaAllocation _allocation;

    public VkBuffer(
        Buffer handle,
        ulong size,
        BufferUsageFlags usage,
        VmaAllocation allocation,
        nint mappedData,
        IVulkanContext context,
        CallerContext callerContext = default) : base(context, callerContext)
    {
        Handle = handle;
        Size = size;
        Usage = usage;
        _allocation = allocation;
        MappedData = mappedData;
    }

    public Buffer Handle { get; }
    public ulong Size { get; }
    public BufferUsageFlags Usage { get; }

    /// <summary>
    /// Mapped CPU pointer captured at create-time when the allocation was made with
    /// <see cref="VmaAllocationCreateFlags.Mapped"/>; zero otherwise.
    /// </summary>
    public nint MappedData { get; }

    public override void Destroy()
    {
        VulkanContext.VmaAllocator.DestroyBuffer(Handle, _allocation);
    }
}
