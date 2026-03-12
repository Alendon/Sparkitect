using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class SoAStorageTests
{
    private static readonly Identification PositionId = TestPosition.Identification;
    private static readonly Identification VelocityId = TestVelocity.Identification;

    private static SoAStorage CreateTestStorage(FakeObjectTracker tracker, IWorld? world = null, int initialCapacity = 64)
    {
        var componentMeta = new (Identification Id, int Size, int Alignment)[]
        {
            (PositionId, sizeof(float) * 2, sizeof(float)),
            (VelocityId, sizeof(float) * 2, sizeof(float))
        };
        return new SoAStorage(componentMeta, tracker, world ?? IWorld.Create(), initialCapacity);
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
        using var storage = CreateTestStorage(tracker, world);

        // Allocate entity and set component data
        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 42f, Y = 99f });
        storage.Set(VelocityId, slot, new TestVelocity { Dx = 1f, Dy = -1f });

        // Register with World using instance capability registrations
        var capabilities = storage.CreateCapabilityRegistrations();
        var handle = world.AddStorage(storage, capabilities);

        // Resolve via IChunkedIteration requirement
        var componentIds = new HashSet<Identification> { PositionId, VelocityId };
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

    [Test]
    public async Task Identity_AssignAndTryResolve_RoundTrip()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var capabilities = storage.CreateCapabilityRegistrations();
        var storageHandle = world.AddStorage(storage, capabilities);
        storage.SetHandle(storageHandle);

        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();

        storage.Assign(entityId, slot);

        var found = storage.TryResolve(entityId, out int resolvedSlot);
        await Assert.That(found).IsTrue();
        await Assert.That(resolvedSlot).IsEqualTo(slot);

        var resolvedId = storage.GetEntityId(slot);
        await Assert.That(resolvedId).IsEqualTo(entityId);
    }

    [Test]
    public async Task Identity_Assign_ThrowsIfSetHandleNotCalled()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var entityId = world.AllocateEntityId();

        await Assert.That(() =>
        {
            storage.Assign(entityId, 0);
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Identity_Assign_CallsWorldBindEntity()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var capabilities = storage.CreateCapabilityRegistrations();
        var storageHandle = world.AddStorage(storage, capabilities);
        storage.SetHandle(storageHandle);

        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();
        storage.Assign(entityId, slot);

        // World should have bound this entity to our storage
        var state = world.GetEntityState(entityId);
        await Assert.That(state).IsEqualTo(EntityState.Bound);

        var boundHandle = world.GetStorageHandle(entityId);
        await Assert.That(boundHandle).IsEqualTo(storageHandle);
    }

    [Test]
    public async Task RemoveEntity_SwapAndPop_PreservesIdentityMappings()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var capabilities = storage.CreateCapabilityRegistrations();
        var storageHandle = world.AddStorage(storage, capabilities);
        storage.SetHandle(storageHandle);

        // Create 3 entities
        var id0 = world.AllocateEntityId();
        var id1 = world.AllocateEntityId();
        var id2 = world.AllocateEntityId();

        var s0 = storage.AllocateEntity();
        var s1 = storage.AllocateEntity();
        var s2 = storage.AllocateEntity();

        storage.Assign(id0, s0);
        storage.Assign(id1, s1);
        storage.Assign(id2, s2);

        storage.Set(PositionId, s0, new TestPosition { X = 10f, Y = 10f });
        storage.Set(PositionId, s1, new TestPosition { X = 20f, Y = 20f });
        storage.Set(PositionId, s2, new TestPosition { X = 30f, Y = 30f });

        // Remove entity at slot 0 -- entity from slot 2 swaps to slot 0
        storage.Unassign(id0);
        storage.RemoveEntity(s0);

        // id2 should now resolve to slot 0 (it was at slot 2, swapped to 0)
        var found2 = storage.TryResolve(id2, out int slot2);
        await Assert.That(found2).IsTrue();
        await Assert.That(slot2).IsEqualTo(0);

        // id1 should still resolve to slot 1 (unchanged)
        var found1 = storage.TryResolve(id1, out int slot1);
        await Assert.That(found1).IsTrue();
        await Assert.That(slot1).IsEqualTo(1);

        // Verify component data was swapped correctly too
        var pos0 = storage.Get<TestPosition>(PositionId, 0);
        await Assert.That(pos0.X).IsEqualTo(30f);
    }

    [Test]
    public async Task Identity_CreateCapabilityRegistrations_IncludesEntityIdentity()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var capabilities = storage.CreateCapabilityRegistrations();

        // Should have 4 registrations: IChunkedIteration, IComponentAccess<int>, IEntityMutation<int>, IEntityIdentity<int>
        await Assert.That(capabilities).HasCount().EqualTo(4);
    }

    [Test]
    public async Task EndToEnd_EntityLifecycle_AllocateAssignDestroyRecycle()
    {
        var tracker = new FakeObjectTracker();
        using var world = IWorld.Create();
        using var storage = CreateTestStorage(tracker, world);

        var capabilities = storage.CreateCapabilityRegistrations();
        var storageHandle = world.AddStorage(storage, capabilities);
        storage.SetHandle(storageHandle);

        // Allocate entity ID and slot, assign identity
        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 100f, Y = 200f });
        storage.Assign(entityId, slot);

        // Verify entity is valid and resolvable
        await Assert.That(world.IsValid(entityId)).IsTrue();
        var found = storage.TryResolve(entityId, out int resolvedSlot);
        await Assert.That(found).IsTrue();
        await Assert.That(resolvedSlot).IsEqualTo(slot);

        // Destroy: unassign, remove from storage, reclaim ID
        storage.Unassign(entityId);
        storage.RemoveEntity(slot);
        world.ReclaimEntityId(entityId);

        // Old ID should be invalid
        await Assert.That(world.IsValid(entityId)).IsFalse();
        var foundAfter = storage.TryResolve(entityId, out _);
        await Assert.That(foundAfter).IsFalse();

        // Allocate new entity -- should reuse the reclaimed slot
        var newEntityId = world.AllocateEntityId();
        var newSlot = storage.AllocateEntity();
        storage.Assign(newEntityId, newSlot);

        // New entity has bumped generation
        await Assert.That(world.IsValid(newEntityId)).IsTrue();
        // Old entity ID remains invalid (generation mismatch)
        await Assert.That(world.IsValid(entityId)).IsFalse();
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
