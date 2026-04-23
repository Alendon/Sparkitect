using Silk.NET.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Sparkitect.Tests.Graphics.Vulkan.Vma;

public class ManagedVmaAllocatorTests
{
    [Test]
    public async Task CreateBuffer_ReturnsWrapperWithRawHandleAndAllocation()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var bufferInfo = new BufferCreateInfo { SType = StructureType.BufferCreateInfo, Size = 1024 };
        var allocInfo = new VmaAllocationCreateInfo { Usage = VmaMemoryUsage.GpuOnly };

        var buffer = allocator.CreateBuffer(in bufferInfo, in allocInfo);

        await Assert.That(buffer).IsNotNull();
        await Assert.That(rawOps.CreatedBuffers.Count).IsEqualTo(1);
        await Assert.That(buffer.Buffer.Handle).IsEqualTo(rawOps.CreatedBuffers[0].Buffer.Handle);
        await Assert.That(buffer.Allocation.Handle).IsEqualTo(rawOps.CreatedBuffers[0].Allocation.Handle);
        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CreateImage_ReturnsWrapperWithRawHandleAndAllocation()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = Format.R8G8B8A8Unorm,
            Extent = new Extent3D(1, 1, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.StorageBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };
        var allocInfo = new VmaAllocationCreateInfo { Usage = VmaMemoryUsage.GpuOnly };

        var image = allocator.CreateImage(in imageInfo, in allocInfo);

        await Assert.That(image).IsNotNull();
        await Assert.That(rawOps.CreatedImages.Count).IsEqualTo(1);
        await Assert.That(image.Image.Handle).IsEqualTo(rawOps.CreatedImages[0].Image.Handle);
        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_VmaBuffer_UntracksFromAllocatorAndDestroysHandle()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var bufferInfo = new BufferCreateInfo { SType = StructureType.BufferCreateInfo, Size = 1024 };
        var allocInfo = new VmaAllocationCreateInfo { Usage = VmaMemoryUsage.GpuOnly };
        var buffer = allocator.CreateBuffer(in bufferInfo, in allocInfo);

        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(1);

        buffer.Dispose();

        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(0);
        await Assert.That(rawOps.DestroyedBuffers.Count).IsEqualTo(1);
        await Assert.That(rawOps.DestroyedBuffers[0].Buffer.Handle).IsEqualTo(buffer.Buffer.Handle);
    }

}
