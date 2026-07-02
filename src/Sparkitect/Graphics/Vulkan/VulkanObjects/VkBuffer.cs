using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

/// <summary>Owns a VMA-allocated buffer and its backing device memory, optionally persistently mapped.</summary>
[PublicAPI]
public class VkBuffer : VulkanObject
{
    private readonly VmaAllocation _allocation;

    /// <summary>Wraps a buffer allocated through VMA, recording its size, usage, and mapped pointer.</summary>
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

    /// <summary>The underlying Silk.NET <see cref="Buffer"/> handle.</summary>
    public Buffer Handle { get; }

    /// <summary>The buffer size in bytes.</summary>
    public ulong Size { get; }

    /// <summary>The usage flags the buffer was created with.</summary>
    public BufferUsageFlags Usage { get; }

    /// <summary>
    /// Mapped CPU pointer captured at create-time when the allocation was made with
    /// <see cref="VmaAllocationCreateFlags.Mapped"/>; zero otherwise.
    /// </summary>
    public nint MappedData { get; }

    /// <inheritdoc/>
    public override void Destroy()
    {
        VulkanContext.VmaAllocator.DestroyBuffer(Handle, _allocation);
    }
}
