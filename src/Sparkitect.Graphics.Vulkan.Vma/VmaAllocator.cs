using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma.Internal;
using Vortice.Vulkan;
using VmaAllocation = Sparkitect.Graphics.Vulkan.Vma.VmaAllocation;

namespace Sparkitect.Graphics.Vulkan.Vma;

public sealed class VmaAllocator : IDisposable
{
    private readonly VorticeAllocator _allocator;
    private bool _disposed;

    private VmaAllocator(VorticeAllocator allocator)
    {
        _allocator = allocator;
    }

    static VmaAllocator()
    {
        Vortice.Vulkan.Vulkan.vkInitialize();
    }

    public static VmaAllocator Create(Instance instance, PhysicalDevice physicalDevice, Device device, uint vulkanApiVersion = 0)
    {
        var createInfo = new VmaAllocatorCreateInfo
        {
            instance = instance.ToVortice(),
            physicalDevice = physicalDevice.ToVortice(),
            device = device.ToVortice(),
            vulkanApiVersion = new VkVersion(vulkanApiVersion),
        };

        Vortice.Vulkan.Vma.vmaCreateAllocator(in createInfo, out var allocator).CheckResult();
        return new VmaAllocator(allocator);
    }

    public unsafe VmaBuffer CreateBuffer(in BufferCreateInfo bufferInfo, in VmaAllocationCreateInfo allocInfo)
    {
        ThrowIfDisposed();

        var vorticeBufferInfo = VmaStructConvert.ToVortice(in bufferInfo);
        var vorticeAllocInfo = VmaStructConvert.ToVortice(in allocInfo, allocInfo.Pool);

        Vortice.Vulkan.Vma.vmaCreateBuffer(
            _allocator,
            &vorticeBufferInfo,
            &vorticeAllocInfo,
            out var buffer,
            out var allocation,
            out var allocInfoOut).CheckResult();

        return new VmaBuffer(
            this,
            buffer.ToSilk(),
            new VmaAllocation(allocation.Handle),
            VmaStructConvert.ToPublic(in allocInfoOut));
    }

    public unsafe VmaImage CreateImage(in ImageCreateInfo imageInfo, in VmaAllocationCreateInfo allocInfo)
    {
        ThrowIfDisposed();

        var vorticeImageInfo = VmaStructConvert.ToVortice(in imageInfo);
        var vorticeAllocInfo = VmaStructConvert.ToVortice(in allocInfo, allocInfo.Pool);

        Vortice.Vulkan.Vma.vmaCreateImage(_allocator, &vorticeImageInfo, &vorticeAllocInfo, out VkImage vkImage,
            out Vortice.Vulkan.VmaAllocation vmaAllocation, out Vortice.Vulkan.VmaAllocationInfo vmaAllocationInfo).CheckResult();

        return new VmaImage(
            this,
            vkImage.ToSilk(),
            new VmaAllocation(vmaAllocation.Handle),
            VmaStructConvert.ToPublic(in vmaAllocationInfo));
    }

    public unsafe VmaPool CreatePool(in VmaPoolCreateInfo poolInfo)
    {
        ThrowIfDisposed();

        var vorticePoolInfo = new Vortice.Vulkan.VmaPoolCreateInfo
        {
            memoryTypeIndex = poolInfo.MemoryTypeIndex,
            flags = (Vortice.Vulkan.VmaPoolCreateFlags)poolInfo.Flags,
            blockSize = poolInfo.BlockSize,
            minBlockCount = poolInfo.MinBlockCount,
            maxBlockCount = poolInfo.MaxBlockCount,
            priority = poolInfo.Priority,
        };

        Vortice.Vulkan.VmaPool pool;
        Vortice.Vulkan.Vma.vmaCreatePool(_allocator, &vorticePoolInfo, &pool).CheckResult();
        return new VmaPool(this, pool.Handle);
    }

    public unsafe VmaDefragmentationContext BeginDefragmentation(VmaDefragmentationFlags flags = VmaDefragmentationFlags.None)
    {
        ThrowIfDisposed();

        var defragInfo = new VmaDefragmentationInfo
        {
            flags = (Vortice.Vulkan.VmaDefragmentationFlags)flags,
        };

        Vortice.Vulkan.VmaDefragmentationContext context;
        Vortice.Vulkan.Vma.vmaBeginDefragmentation(_allocator, &defragInfo, &context).CheckResult();
        return new VmaDefragmentationContext(this, context.Handle);
    }

    public unsafe nint MapMemory(VmaAllocation allocation)
    {
        ThrowIfDisposed();
        void* data;
        Vortice.Vulkan.Vma.vmaMapMemory(_allocator, new Vortice.Vulkan.VmaAllocation(allocation.Handle), &data).CheckResult();
        return (nint)data;
    }

    public void UnmapMemory(VmaAllocation allocation)
    {
        ThrowIfDisposed();
        Vortice.Vulkan.Vma.vmaUnmapMemory(_allocator, new Vortice.Vulkan.VmaAllocation(allocation.Handle));
    }

    public unsafe VmaAllocationInfo GetAllocationInfo(VmaAllocation allocation)
    {
        ThrowIfDisposed();
        Vortice.Vulkan.VmaAllocationInfo info;
        Vortice.Vulkan.Vma.vmaGetAllocationInfo(_allocator, new Vortice.Vulkan.VmaAllocation(allocation.Handle), &info);
        return VmaStructConvert.ToPublic(in info);
    }

    internal void DestroyBuffer(VmaBuffer buffer)
    {
        if (_disposed) return;
        Vortice.Vulkan.Vma.vmaDestroyBuffer(_allocator, buffer.Buffer.ToVortice(), new Vortice.Vulkan.VmaAllocation(buffer.Allocation.Handle));
    }

    internal void DestroyImage(VmaImage image)
    {
        if (_disposed) return;
        Vortice.Vulkan.Vma.vmaDestroyImage(_allocator, image.Image.ToVortice(), new Vortice.Vulkan.VmaAllocation(image.Allocation.Handle));
    }

    internal void DestroyPool(VmaPool pool)
    {
        if (_disposed) return;
        Vortice.Vulkan.Vma.vmaDestroyPool(_allocator, new Vortice.Vulkan.VmaPool(pool.Handle));
    }

    internal unsafe void EndDefragmentation(VmaDefragmentationContext context)
    {
        if (_disposed) return;
        Vortice.Vulkan.VmaDefragmentationStats stats;
        Vortice.Vulkan.Vma.vmaEndDefragmentation(_allocator, new Vortice.Vulkan.VmaDefragmentationContext(context.Handle), &stats);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Vortice.Vulkan.Vma.vmaDestroyAllocator(_allocator);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
