using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Modding;

namespace Sparkitect.Tests.RenderGraph;

public class ResourceRegistrationStoreTests
{
    private static readonly Identification ImageA = Identification.Create(1, 1, 1);
    private static readonly Identification ImageB = Identification.Create(1, 1, 2);
    private static readonly Identification BufferA = Identification.Create(1, 2, 1);
    private static readonly Identification BufferB = Identification.Create(1, 2, 2);

    private static ImageDescription MakeDescription(uint width = 800, uint height = 600) =>
        new(new Extent2D(width, height), Format.R8G8B8A8Unorm, Transient: false, DefaultFill: null);

    private static BufferDescription MakeBufferDescription(ulong stride = 16, ulong capacity = 256) =>
        new(stride, capacity);

    [Test]
    public async Task RegisterImage_ThenTryGetImage_RoundTrips()
    {
        var store = new ResourceRegistrationStore();
        var desc = MakeDescription();

        store.RegisterImage(ImageA, desc);

        var found = store.TryGetImage(ImageA, out var got);

        await Assert.That(found).IsTrue();
        await Assert.That(got).IsEqualTo(desc);
    }

    [Test]
    public async Task TryGetImage_UnregisteredId_ReturnsFalse()
    {
        var store = new ResourceRegistrationStore();

        var found = store.TryGetImage(ImageA, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task RegisteredImages_EnumeratesAllRegistrations()
    {
        var store = new ResourceRegistrationStore();
        var descA = MakeDescription(800, 600);
        var descB = MakeDescription(1024, 768);

        store.RegisterImage(ImageA, descA);
        store.RegisterImage(ImageB, descB);

        IResourceRegistrationStore typed = store;

        await Assert.That(typed.RegisteredImages.Count).IsEqualTo(2);
        await Assert.That(typed.RegisteredImages[ImageA]).IsEqualTo(descA);
        await Assert.That(typed.RegisteredImages[ImageB]).IsEqualTo(descB);
    }

    [Test]
    public async Task RegisterBuffer_ThenTryGetBuffer_RoundTrips()
    {
        var store = new ResourceRegistrationStore();
        var desc = MakeBufferDescription();

        store.RegisterBuffer(BufferA, desc);

        var found = store.TryGetBuffer(BufferA, out var got);

        await Assert.That(found).IsTrue();
        await Assert.That(got).IsEqualTo(desc);
    }

    [Test]
    public async Task TryGetBuffer_UnregisteredId_ReturnsFalse()
    {
        var store = new ResourceRegistrationStore();

        var found = store.TryGetBuffer(BufferA, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task RegisteredBuffers_EnumeratesAllRegistrations()
    {
        var store = new ResourceRegistrationStore();
        var descA = MakeBufferDescription(16, 256);
        var descB = MakeBufferDescription(32, 512);

        store.RegisterBuffer(BufferA, descA);
        store.RegisterBuffer(BufferB, descB);

        IResourceRegistrationStore typed = store;

        await Assert.That(typed.RegisteredBuffers.Count).IsEqualTo(2);
        await Assert.That(typed.RegisteredBuffers[BufferA]).IsEqualTo(descA);
        await Assert.That(typed.RegisteredBuffers[BufferB]).IsEqualTo(descB);
    }
}
