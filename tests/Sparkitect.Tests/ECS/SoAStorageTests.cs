using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class SoAStorageTests
{
    private static readonly Identification PositionId = TestPosition.Identification;
    private static readonly Identification VelocityId = TestVelocity.Identification;

    private static SoAStorage CreateTestStorage(FakeObjectTracker tracker, int initialCapacity = 64)
    {
        var componentMeta = new (Identification Id, int Size, int Alignment)[]
        {
            (PositionId, sizeof(float) * 2, sizeof(float)),
            (VelocityId, sizeof(float) * 2, sizeof(float))
        };
        return new SoAStorage(componentMeta, tracker, initialCapacity);
    }

    [Test]
    public async Task AllocateEntity_ReturnsSequentialSlotIndices()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        var s0 = storage.AllocateEntity();
        var s1 = storage.AllocateEntity();
        var s2 = storage.AllocateEntity();

        await Assert.That(s0).IsEqualTo(0);
        await Assert.That(s1).IsEqualTo(1);
        await Assert.That(s2).IsEqualTo(2);
    }

    [Test]
    public async Task ComponentAccess_SetThenGet_ReturnsSameValue()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 1.0f, Y = 2.0f });

        var pos = storage.Get<TestPosition>(PositionId, slot);

        await Assert.That(pos.X).IsEqualTo(1.0f);
        await Assert.That(pos.Y).IsEqualTo(2.0f);
    }

    [Test]
    public async Task RemoveEntity_SwapAndPop_LastEntityMovesToRemovedSlot()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        var s0 = storage.AllocateEntity();
        var s1 = storage.AllocateEntity();
        var s2 = storage.AllocateEntity();

        storage.Set(PositionId, s0, new TestPosition { X = 10f, Y = 10f });
        storage.Set(PositionId, s1, new TestPosition { X = 20f, Y = 20f });
        storage.Set(PositionId, s2, new TestPosition { X = 30f, Y = 30f });

        storage.RemoveEntity(s0); // Remove first, last (30,30) moves to index 0

        // After removal: slot 0 has entity from slot 2, slot 1 unchanged
        var pos0 = storage.Get<TestPosition>(PositionId, 0);
        var pos1 = storage.Get<TestPosition>(PositionId, 1);

        await Assert.That(pos0.X).IsEqualTo(30f);
        await Assert.That(pos0.Y).IsEqualTo(30f);
        await Assert.That(pos1.X).IsEqualTo(20f);
        await Assert.That(pos1.Y).IsEqualTo(20f);
    }

    [Test]
    public async Task RemoveEntity_LastEntity_JustDecrementsCount()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        storage.AllocateEntity();
        var s1 = storage.AllocateEntity();

        storage.Set(PositionId, 0, new TestPosition { X = 10f, Y = 10f });
        storage.Set(PositionId, 1, new TestPosition { X = 20f, Y = 20f });

        storage.RemoveEntity(s1); // Remove last, no swap

        // First entity is unchanged, total count should be 1
        var handle = new ChunkHandle();
        storage.GetNextChunk(ref handle, out int length);
        await Assert.That(length).IsEqualTo(1);

        var pos0 = storage.Get<TestPosition>(PositionId, 0);
        await Assert.That(pos0.X).IsEqualTo(10f);
    }

    [Test]
    public async Task GetNextChunk_IteratesAllEntitiesInSingleChunk()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        storage.AllocateEntity();
        storage.AllocateEntity();
        storage.AllocateEntity();

        var handle = new ChunkHandle();
        var hasChunk = storage.GetNextChunk(ref handle, out int length);

        await Assert.That(hasChunk).IsTrue();
        await Assert.That(length).IsEqualTo(3);

        // Second call returns false (dense storage = single chunk)
        var hasMore = storage.GetNextChunk(ref handle, out _);
        await Assert.That(hasMore).IsFalse();
    }

    [Test]
    public async Task GetNextChunk_EmptyStorage_ReturnsFalse()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        var handle = new ChunkHandle();
        var hasChunk = storage.GetNextChunk(ref handle, out int length);

        await Assert.That(hasChunk).IsFalse();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task GetChunkComponentData_ReturnsPointerToColumnData()
    {
        var tracker = new FakeObjectTracker();
        using var storage = CreateTestStorage(tracker);

        storage.AllocateEntity();
        storage.Set(PositionId, 0, new TestPosition { X = 5f, Y = 6f });

        var handle = new ChunkHandle();
        storage.GetNextChunk(ref handle, out _);

        float x;
        unsafe
        {
            byte* ptr = storage.GetChunkComponentData(ref handle, PositionId);
            var pos = *(TestPosition*)ptr;
            x = pos.X;
        }

        await Assert.That(x).IsEqualTo(5f);
    }

    [Test]
    public async Task Dispose_DisposesAllNativeColumns()
    {
        var tracker = new FakeObjectTracker();
        var storage = CreateTestStorage(tracker);

        storage.AllocateEntity();

        // tracker tracks 2 columns (Position + Velocity)
        await Assert.That(tracker.TrackCount).IsEqualTo(2);

        storage.Dispose();

        // Both columns should have been untracked
        await Assert.That(tracker.UntrackCount).IsEqualTo(2);
    }

    [Test]
    public async Task Integration_WorldRegistration_ResolveViaCapabilities()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker);

        // Allocate entity and set component data
        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 42f, Y = 99f });
        storage.Set(VelocityId, slot, new TestVelocity { Dx = 1f, Dy = -1f });

        // Register with World using capability registrations
        var componentIds = new HashSet<Identification> { PositionId, VelocityId };
        var capabilities = SoAStorage.CreateCapabilityRegistrations(componentIds);
        var handle = world.AddStorage(storage, capabilities);

        // Resolve via IChunkedIteration requirement
        var requirement = new ChunkedIterationRequirement(componentIds);
        var results = world.Resolve(new ICapabilityRequirement[] { requirement });

        await Assert.That(results).HasCount().EqualTo(1);
        await Assert.That(results[0]).IsEqualTo(handle);

        // Access storage through StorageAccessor -- ref struct, must not cross await
        // Extract capabilities before any await
        var accessor = world.GetStorage(handle);
        var chunked = accessor.As<IChunkedIteration>();
        var componentAccess = accessor.As<IComponentAccess<int>>();

        await Assert.That(chunked).IsNotNull();
        await Assert.That(componentAccess).IsNotNull();

        // Iterate entities via chunks
        var chunkHandle = new ChunkHandle();
        var hasChunk = chunked!.GetNextChunk(ref chunkHandle, out int length);
        await Assert.That(hasChunk).IsTrue();
        await Assert.That(length).IsEqualTo(1);

        // Verify component data via component access
        var pos = componentAccess!.Get<TestPosition>(PositionId, 0);
        await Assert.That(pos.X).IsEqualTo(42f);
        await Assert.That(pos.Y).IsEqualTo(99f);
    }
}

/// <summary>
/// Test capability requirement for IChunkedIteration resolution.
/// </summary>
public class ChunkedIterationRequirement : ICapabilityRequirement<IChunkedIteration, ComponentSetMetadata>
{
    private readonly IReadOnlySet<Identification> _requiredComponents;

    public ChunkedIterationRequirement(IReadOnlySet<Identification> requiredComponents)
    {
        _requiredComponents = requiredComponents;
    }

    public bool Matches(ComponentSetMetadata metadata)
    {
        return _requiredComponents.IsSubsetOf(metadata.Components);
    }
}
