using System.Linq;
using Sparkitect.Graphics.Vulkan.Vma;
using Sparkitect.Utils;

namespace Sparkitect.Tests.Graphics.Vulkan.Vma;

public class VmaResourceTests
{
    [Test]
    public async Task Constructor_RegistersOnAllocatorTracker()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var callsite = new CallerContext("VmaResourceTests.cs", 42);
        _ = new TestVmaResource(allocator, callsite);

        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_UntracksFromAllocatorAndCallsDestroy()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var resource = new TestVmaResource(allocator);
        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(1);

        resource.Dispose();

        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(0);
        await Assert.That(resource.DestroyCallCount).IsEqualTo(1);
        await Assert.That(resource.IsDisposed).IsEqualTo(true);
    }

    [Test]
    public async Task Dispose_IsIdempotent()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var resource = new TestVmaResource(allocator);

        resource.Dispose();
        resource.Dispose();   // second call — MUST NOT throw, MUST NOT double-invoke Destroy, MUST NOT double-untrack
        resource.Dispose();   // third call for good measure

        await Assert.That(resource.DestroyCallCount).IsEqualTo(1);
        await Assert.That(allocator.ObjectTracker.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_CallerContextForwardedToAllocatorTracker()
    {
        var rawOps = new FakeVmaRawOps();
        using var allocator = new ManagedVmaAllocator(rawOps);

        var callsite = new CallerContext("FooBar.cs", 99);
        _ = new TestVmaResource(allocator, callsite);

        var entries = allocator.ObjectTracker.GetTrackingEntries().ToList();
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Callsite.FilePath).IsEqualTo("FooBar.cs");
        await Assert.That(entries[0].Callsite.LineNumber).IsEqualTo(99);
    }
}
