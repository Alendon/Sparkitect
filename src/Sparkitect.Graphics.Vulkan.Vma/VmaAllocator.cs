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

    /// <summary>
    /// Creates a new VMA allocator bound to the given Vulkan instance / physical device / device.
    /// </summary>
    /// <param name="instance">The Vulkan instance.</param>
    /// <param name="physicalDevice">The physical device.</param>
    /// <param name="device">The logical device.</param>
    /// <param name="vulkanApiVersion">The Vulkan API version used at instance creation (pass <c>Silk.NET.Vulkan.Vk.Version13</c> from the caller; 0 is legal for legacy code paths).</param>
    /// <returns>A new raw VMA allocator wrapper.</returns>
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

    /// <summary>
    /// Creates a VMA-backed Vulkan buffer + allocation pair.
    /// </summary>
    /// <remarks>
    /// Out-params return the raw <see cref="Silk.NET.Vulkan.Buffer"/> handle, the opaque
    /// <see cref="VmaAllocation"/>, and the <see cref="VmaAllocationInfo"/> describing the allocation.
    /// The caller (normally a managed wrapper in main Sparkitect) pairs them and owns the lifetime.
    /// </remarks>
    public unsafe void CreateBuffer(
        in BufferCreateInfo bufferInfo,
        in VmaAllocationCreateInfo allocInfo,
        out Silk.NET.Vulkan.Buffer buffer,
        out VmaAllocation allocation,
        out VmaAllocationInfo allocationInfo)
    {
        ThrowIfDisposed();

        var vorticeBufferInfo = VmaStructConvert.ToVortice(in bufferInfo);
        var vorticeAllocInfo = VmaStructConvert.ToVortice(in allocInfo, allocInfo.Pool);

        Vortice.Vulkan.Vma.vmaCreateBuffer(
            _allocator,
            &vorticeBufferInfo,
            &vorticeAllocInfo,
            out var rawBuffer,
            out var rawAlloc,
            out var rawAllocInfo).CheckResult();

        buffer = rawBuffer.ToSilk();
        allocation = new VmaAllocation(rawAlloc.Handle);
        allocationInfo = VmaStructConvert.ToPublic(in rawAllocInfo);
    }

    /// <summary>
    /// Creates a VMA-backed Vulkan image + allocation pair.
    /// </summary>
    /// <remarks>
    /// Out-params return the raw <see cref="Silk.NET.Vulkan.Image"/> handle, the opaque
    /// <see cref="VmaAllocation"/>, and the <see cref="VmaAllocationInfo"/>.
    /// </remarks>
    public unsafe void CreateImage(
        in ImageCreateInfo imageInfo,
        in VmaAllocationCreateInfo allocInfo,
        out Image image,
        out VmaAllocation allocation,
        out VmaAllocationInfo allocationInfo)
    {
        ThrowIfDisposed();

        var vorticeImageInfo = VmaStructConvert.ToVortice(in imageInfo);
        var vorticeAllocInfo = VmaStructConvert.ToVortice(in allocInfo, allocInfo.Pool);

        Vortice.Vulkan.Vma.vmaCreateImage(
            _allocator,
            &vorticeImageInfo,
            &vorticeAllocInfo,
            out var rawImage,
            out var rawAlloc,
            out var rawAllocInfo).CheckResult();

        image = rawImage.ToSilk();
        allocation = new VmaAllocation(rawAlloc.Handle);
        allocationInfo = VmaStructConvert.ToPublic(in rawAllocInfo);
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

    /// <summary>
    /// Destroys a VMA-backed buffer + allocation. Idempotent once the allocator itself is disposed.
    /// Public to allow managed wrappers in main Sparkitect (see <c>ManagedVmaAllocator</c>) to call
    /// without requiring <c>InternalsVisibleTo</c>.
    /// </summary>
    public unsafe void DestroyBuffer(Silk.NET.Vulkan.Buffer buffer, VmaAllocation allocation)
    {
        if (_disposed) return;
        Vortice.Vulkan.Vma.vmaDestroyBuffer(_allocator, buffer.ToVortice(), new Vortice.Vulkan.VmaAllocation(allocation.Handle));
    }

    /// <summary>
    /// Destroys a VMA-backed image + allocation. Idempotent once the allocator itself is disposed.
    /// Public for the same reason as <see cref="DestroyBuffer"/>.
    /// </summary>
    public unsafe void DestroyImage(Silk.NET.Vulkan.Image image, VmaAllocation allocation)
    {
        if (_disposed) return;
        Vortice.Vulkan.Vma.vmaDestroyImage(_allocator, image.ToVortice(), new Vortice.Vulkan.VmaAllocation(allocation.Handle));
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
