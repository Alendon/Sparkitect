using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class ComponentQueryTests
{
    private static readonly Identification PositionId = TestPosition.Identification;
    private static readonly Identification VelocityId = TestVelocity.Identification;

    // --- Iteration Tests ---

    [Test]
    public async Task ComponentQuery_IteratesSingleStorage_ReturnsCorrectData()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId, VelocityId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Add 3 entities with known values
        for (int i = 0; i < 3; i++)
        {
            var slot = storage.AllocateEntity();
            storage.Set(slot, new TestPosition { X = i * 10f, Y = i * 20f });
            storage.Set(slot, new TestVelocity { Dx = i * 1f, Dy = i * 2f });
        }

        var metadata = new ComponentQueryMetadata([PositionId, VelocityId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        var positions = new List<TestPosition>();
        foreach (var entity in query)
        {
            positions.Add(entity.Get<TestPosition>());
        }

        await Assert.That(positions).Count().IsEqualTo(3);
        await Assert.That(positions[0].X).IsEqualTo(0f);
        await Assert.That(positions[1].X).IsEqualTo(10f);
        await Assert.That(positions[2].X).IsEqualTo(20f);
    }

    [Test]
    public async Task ComponentQuery_IteratesMultipleStorages_ReturnsAllEntities()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();

        // Storage 1: 2 entities
        using var storage1 = CreateStorage(tracker, world, [PositionId, VelocityId]);
        var handle1 = world.AddStorage(storage1, storage1.CreateCapabilityRegistrations());
        storage1.SetHandle(handle1);
        for (int i = 0; i < 2; i++)
        {
            var slot = storage1.AllocateEntity();
            storage1.Set(slot, new TestPosition { X = i, Y = 0 });
        }

        // Storage 2: 3 entities
        using var storage2 = CreateStorage(tracker, world, [PositionId, VelocityId]);
        var handle2 = world.AddStorage(storage2, storage2.CreateCapabilityRegistrations());
        storage2.SetHandle(handle2);
        for (int i = 0; i < 3; i++)
        {
            var slot = storage2.AllocateEntity();
            storage2.Set(slot, new TestPosition { X = 100 + i, Y = 0 });
        }

        var metadata = new ComponentQueryMetadata([PositionId, VelocityId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        var xValues = new List<float>();
        foreach (var entity in query)
        {
            xValues.Add(entity.Get<TestPosition>().X);
        }

        await Assert.That(xValues).Count().IsEqualTo(5);
        // First storage entities
        await Assert.That(xValues[0]).IsEqualTo(0f);
        await Assert.That(xValues[1]).IsEqualTo(1f);
        // Second storage entities
        await Assert.That(xValues[2]).IsEqualTo(100f);
        await Assert.That(xValues[3]).IsEqualTo(101f);
        await Assert.That(xValues[4]).IsEqualTo(102f);
    }

    [Test]
    public async Task ComponentQuery_EmptyEnumeration_WhenNoStoragesMatch()
    {
        using var world = IWorld.Create();
        // No storages added

        var metadata = new ComponentQueryMetadata([PositionId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        var count = 0;
        foreach (var _ in query)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
    }

    // --- EntityAccessor Tests ---

    [Test]
    public async Task EntityAccessor_GetRef_ReturnsMutableReference()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var slot = storage.AllocateEntity();
        storage.Set(slot, new TestPosition { X = 42f, Y = 99f });

        var metadata = new ComponentQueryMetadata([PositionId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        foreach (var entity in query)
        {
            ref var pos = ref entity.GetRef<TestPosition>();
            pos.X = 777f;
        }

        // Verify mutation persisted in the underlying storage
        var readBack = storage.Get<TestPosition>(0);
        await Assert.That(readBack.X).IsEqualTo(777f);
    }

    [Test]
    public async Task EntityAccessor_Get_ReturnsCopy()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var slot = storage.AllocateEntity();
        storage.Set(slot, new TestPosition { X = 42f, Y = 99f });

        var metadata = new ComponentQueryMetadata([PositionId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        TestPosition copy = default;
        foreach (var entity in query)
        {
            copy = entity.Get<TestPosition>();
            // Modifying copy should not affect storage
            copy.X = 999f;
        }

        var readBack = storage.Get<TestPosition>(0);
        await Assert.That(readBack.X).IsEqualTo(42f);
        await Assert.That(copy.X).IsEqualTo(999f);
    }

    // --- Topology Reaction Tests ---

    [Test]
    public async Task ComponentQuery_ReactsToTopologyChange_NewStorageIncluded()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();

        // Create query before adding storage
        var metadata = new ComponentQueryMetadata([PositionId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        // Verify empty initially
        var countBefore = 0;
        foreach (var _ in query) countBefore++;
        await Assert.That(countBefore).IsEqualTo(0);

        // Add storage with matching component AFTER query creation
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var slot = storage.AllocateEntity();
        storage.Set(slot, new TestPosition { X = 55f, Y = 0f });

        // Query should now see the new storage
        var countAfter = 0;
        float xValue = 0;
        foreach (var entity in query)
        {
            xValue = entity.Get<TestPosition>().X;
            countAfter++;
        }

        await Assert.That(countAfter).IsEqualTo(1);
        await Assert.That(xValue).IsEqualTo(55f);
    }

    // --- Dispose Tests ---

    [Test]
    public async Task ComponentQuery_Dispose_UnregistersFilter()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();

        var metadata = new ComponentQueryMetadata([PositionId]);
        var query = (ComponentQuery)metadata.CreateQuery(world);

        // Dispose the query
        query.Dispose();

        // Adding a storage after dispose should not throw
        // (filter is unregistered, so callback won't fire on disposed query)
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // If we got here without exception, the filter was properly unregistered
        await Assert.That(true).IsTrue();
    }

    // --- ComponentSetRequirement Tests ---

    [Test]
    public async Task ComponentSetRequirement_Matches_WhenStorageHasAllComponents()
    {
        var requirement = new ComponentSetRequirement([PositionId, VelocityId]);
        var metadata = new ComponentSetMetadata(new HashSet<Identification> { PositionId, VelocityId });

        var result = requirement.Matches(metadata);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ComponentSetRequirement_DoesNotMatch_WhenMissingComponent()
    {
        var requirement = new ComponentSetRequirement([PositionId, VelocityId]);
        var metadata = new ComponentSetMetadata(new HashSet<Identification> { PositionId });

        var result = requirement.Matches(metadata);

        await Assert.That(result).IsFalse();
    }

    // --- Helpers ---

    private static SoAStorage CreateStorage(
        FakeObjectTracker tracker,
        IWorld world,
        Identification[] componentIds)
    {
        var componentMeta = new List<(Identification Id, int Size)>();
        foreach (var id in componentIds)
        {
            // TestPosition and TestVelocity are both 8 bytes (2 floats)
            componentMeta.Add((id, sizeof(float) * 2));
        }
        return new SoAStorage(componentMeta, tracker, world);
    }
}
