using Sparkitect.ECS;

namespace Sparkitect.Tests.ECS;

public class WorldEntityTests
{
    [Test]
    public async Task AllocateEntityId_ReturnsEntityWithGeneration1()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();

        await Assert.That(id.IsNone).IsFalse();
        await Assert.That(id.Generation).IsEqualTo(1u);
    }

    [Test]
    public async Task AllocateEntityId_SequentialIndices()
    {
        using var world = IWorld.Create();

        var a = world.AllocateEntityId();
        var b = world.AllocateEntityId();
        var c = world.AllocateEntityId();

        await Assert.That(a.Index).IsEqualTo(0u);
        await Assert.That(b.Index).IsEqualTo(1u);
        await Assert.That(c.Index).IsEqualTo(2u);
    }

    [Test]
    public async Task IsValid_AllocatedEntity_ReturnsTrue()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();

        await Assert.That(world.IsValid(id)).IsTrue();
    }

    [Test]
    public async Task IsValid_None_ReturnsFalse()
    {
        using var world = IWorld.Create();

        await Assert.That(world.IsValid(EntityId.None)).IsFalse();
    }

    [Test]
    public async Task GetEntityState_Allocated_ReturnsEmpty()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();

        await Assert.That(world.GetEntityState(id)).IsEqualTo(EntityState.Empty);
    }

    [Test]
    public async Task BindEntity_TransitionsStateToBound()
    {
        using var world = IWorld.Create();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, []);

        var id = world.AllocateEntityId();
        world.BindEntity(id, handle);

        await Assert.That(world.GetEntityState(id)).IsEqualTo(EntityState.Bound);
    }

    [Test]
    public async Task GetStorageHandle_AfterBind_ReturnsCorrectHandle()
    {
        using var world = IWorld.Create();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, []);

        var id = world.AllocateEntityId();
        world.BindEntity(id, handle);

        await Assert.That(world.GetStorageHandle(id)).IsEqualTo(handle);
    }

    [Test]
    public async Task GetStorage_EntityId_ReturnsValidAccessor()
    {
        using var world = IWorld.Create();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, []);

        var id = world.AllocateEntityId();
        world.BindEntity(id, handle);

        var accessor = world.GetStorage(id);

        await Assert.That(accessor.Handle).IsEqualTo(handle);
    }

    [Test]
    public async Task ReclaimEntityId_ValidEntity_ReturnsTrue()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();

        await Assert.That(world.ReclaimEntityId(id)).IsTrue();
    }

    [Test]
    public async Task ReclaimEntityId_SetsStateToNull()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();
        world.ReclaimEntityId(id);

        await Assert.That(world.GetEntityState(id)).IsEqualTo(EntityState.Null);
    }

    [Test]
    public async Task IsValid_AfterReclaim_ReturnsFalse()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();
        world.ReclaimEntityId(id);

        await Assert.That(world.IsValid(id)).IsFalse();
    }

    [Test]
    public async Task ReclaimEntityId_AlreadyDead_ReturnsFalse()
    {
        using var world = IWorld.Create();

        var id = world.AllocateEntityId();
        world.ReclaimEntityId(id);

        await Assert.That(world.ReclaimEntityId(id)).IsFalse();
    }

    [Test]
    public async Task FreeListRecycling_ReusesIndexWithBumpedGeneration()
    {
        using var world = IWorld.Create();

        var first = world.AllocateEntityId();
        world.ReclaimEntityId(first);
        var second = world.AllocateEntityId();

        await Assert.That(second.Index).IsEqualTo(first.Index);
        // Reclaim bumps generation (invalidation) + allocate bumps again (new generation)
        await Assert.That(second.Generation).IsGreaterThan(first.Generation);
    }

    [Test]
    public async Task StaleEntityId_FailsIsValid_AfterRecycle()
    {
        using var world = IWorld.Create();

        var first = world.AllocateEntityId();
        world.ReclaimEntityId(first);
        _ = world.AllocateEntityId(); // reuses slot

        await Assert.That(world.IsValid(first)).IsFalse();
    }

    [Test]
    public async Task TryReclaimEntityId_MatchingStorageHandle_Succeeds()
    {
        using var world = IWorld.Create();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, []);

        var id = world.AllocateEntityId();
        world.BindEntity(id, handle);

        await Assert.That(world.TryReclaimEntityId(id, handle)).IsTrue();
        await Assert.That(world.GetEntityState(id)).IsEqualTo(EntityState.Null);
    }

    [Test]
    public async Task TryReclaimEntityId_MismatchedStorageHandle_Fails()
    {
        using var world = IWorld.Create();
        var storage1 = new TestStorage();
        var storage2 = new TestStorage();
        var handle1 = world.AddStorage(storage1, []);
        var handle2 = world.AddStorage(storage2, []);

        var id = world.AllocateEntityId();
        world.BindEntity(id, handle1);

        await Assert.That(world.TryReclaimEntityId(id, handle2)).IsFalse();
        await Assert.That(world.IsValid(id)).IsTrue(); // still alive
    }

    [Test]
    public async Task GetEntityState_NeverAllocatedIndex_ReturnsNull()
    {
        using var world = IWorld.Create();

        // Allocate one entity to establish pool, then check state for EntityId.None
        _ = world.AllocateEntityId();

        await Assert.That(world.GetEntityState(EntityId.None)).IsEqualTo(EntityState.Null);
    }

    [Test]
    public async Task EntityPool_GrowsWhenCapacityExceeded()
    {
        using var world = IWorld.Create();

        // InitialCapacity is 4, so allocating 5 should trigger growth
        var ids = new EntityId[5];
        for (int i = 0; i < 5; i++)
        {
            ids[i] = world.AllocateEntityId();
        }

        // All should be valid
        for (int i = 0; i < 5; i++)
        {
            await Assert.That(world.IsValid(ids[i])).IsTrue();
        }
    }
}
