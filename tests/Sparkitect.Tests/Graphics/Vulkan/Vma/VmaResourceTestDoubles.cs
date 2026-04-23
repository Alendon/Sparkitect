using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Utils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Tests.Graphics.Vulkan.Vma;

/// <summary>
/// Deterministic <see cref="IVmaRawOps"/> double. Every Create produces handles derived from a
/// monotonically increasing counter so tests can distinguish allocations. Records every invocation
/// in public lists for assertion.
/// </summary>
internal sealed class FakeVmaRawOps : IVmaRawOps
{
    private ulong _nextHandle = 1;
    public bool Disposed { get; private set; }

    public List<(Buffer Buffer, VmaAllocation Allocation)> CreatedBuffers { get; } = new();
    public List<(Image Image, VmaAllocation Allocation)> CreatedImages { get; } = new();
    public List<(Buffer Buffer, VmaAllocation Allocation)> DestroyedBuffers { get; } = new();
    public List<(Image Image, VmaAllocation Allocation)> DestroyedImages { get; } = new();

    public void CreateBuffer(in BufferCreateInfo bufferInfo, in VmaAllocationCreateInfo allocInfo,
        out Buffer buffer, out VmaAllocation allocation, out VmaAllocationInfo allocationInfo)
    {
        buffer = new Buffer(_nextHandle++);
        allocation = new VmaAllocation((nint)_nextHandle++);
        allocationInfo = default;
        CreatedBuffers.Add((buffer, allocation));
    }

    public void CreateImage(in ImageCreateInfo imageInfo, in VmaAllocationCreateInfo allocInfo,
        out Image image, out VmaAllocation allocation, out VmaAllocationInfo allocationInfo)
    {
        image = new Image(_nextHandle++);
        allocation = new VmaAllocation((nint)_nextHandle++);
        allocationInfo = default;
        CreatedImages.Add((image, allocation));
    }

    public nint MapMemory(VmaAllocation allocation) => allocation.Handle;
    public void UnmapMemory(VmaAllocation allocation) { }

    public void DestroyBuffer(Buffer buffer, VmaAllocation allocation)
        => DestroyedBuffers.Add((buffer, allocation));

    public void DestroyImage(Image image, VmaAllocation allocation)
        => DestroyedImages.Add((image, allocation));

    public void Dispose() => Disposed = true;
}

/// <summary>
/// Minimal <see cref="VmaResource"/> subclass used by <see cref="VmaResourceTests"/>. Records
/// the number of <see cref="Destroy"/> invocations so tests can assert idempotency.
/// </summary>
public sealed class TestVmaResource : VmaResource
{
    public int DestroyCallCount { get; private set; }

    public TestVmaResource(ManagedVmaAllocator allocator, CallerContext callerContext = default)
        : base(allocator, callerContext)
    {
    }

    public override void Destroy() => DestroyCallCount++;
}
