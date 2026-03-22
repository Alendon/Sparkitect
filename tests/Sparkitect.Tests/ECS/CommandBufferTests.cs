using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Commands;
using Sparkitect.ECS.Queries;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class CommandBufferTests
{
    private static readonly Identification PositionId = TestPosition.Identification;
    private static readonly Identification VelocityId = TestVelocity.Identification;

    // --- Recording Tests ---

    [Test]
    public async Task SetComponent_Extension_AddsSetComponentCommandToBuffer()
    {
        var buffer = new CommandBuffer<int>(EntityId.None, default, isCreate: false);

        buffer.SetComponent(PositionId, new TestPosition { X = 1f, Y = 2f });

        await Assert.That(buffer.Commands).HasCount().EqualTo(1);
        await Assert.That(buffer.Commands[0]).IsAssignableTo<SetComponentCommand<int, TestPosition>>();
    }

    [Test]
    public async Task DestroyEntity_Extension_AddsDestroyEntityCommandToBuffer()
    {
        var buffer = new CommandBuffer<int>(EntityId.None, default, isCreate: false);

        buffer.DestroyEntity();

        await Assert.That(buffer.Commands).HasCount().EqualTo(1);
        await Assert.That(buffer.Commands[0]).IsAssignableTo<DestroyEntityCommand<int>>();
    }

    [Test]
    public async Task DelegateCommand_ExecutesActionDelegate()
    {
        var executed = false;
        IWorld? capturedWorld = null;
        var cmd = new DelegateCommand((world, handle, entityId) =>
        {
            executed = true;
            capturedWorld = world;
        });

        using var world = IWorld.Create();
        cmd.Execute(world, default, EntityId.None);

        await Assert.That(executed).IsTrue();
        await Assert.That(capturedWorld).IsNotNull();
    }

    // --- Accessor Create Tests ---

    [Test]
    public async Task Create_AllocatesEntityId_NotNone()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var buffer = accessor.Create<int>(handle);

        await Assert.That(buffer.EntityId.IsNone).IsFalse();
        await Assert.That(buffer.EntityId.Generation).IsGreaterThanOrEqualTo((uint)1);
    }

    [Test]
    public async Task Create_WithFilter_ResolvesStorageHandle()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId, VelocityId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        ICapabilityRequirement[] filter = [new ComponentSetRequirement([PositionId, VelocityId])];
        var buffer = accessor.Create<int>(filter);

        await Assert.That(buffer.EntityId.IsNone).IsFalse();
        await Assert.That(buffer.StorageHandle).IsEqualTo(handle);
    }

    // --- Accessor Modify Tests ---

    [Test]
    public async Task Modify_ThrowsOnEntityNotBound()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        // Allocate but don't bind -- stays in Empty state
        var entityId = world.AllocateEntityId();

        await Assert.That(() =>
        {
            accessor.Modify<int>(entityId);
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Modify_ResolvesStorageHandleFromWorld()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Create and bind an entity
        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();
        storage.Assign(entityId, slot);

        var buffer = accessor.Modify<int>(entityId);

        await Assert.That(buffer.EntityId).IsEqualTo(entityId);
        await Assert.That(buffer.StorageHandle).IsEqualTo(handle);
    }

    // --- Playback Tests ---

    [Test]
    public async Task Playback_CreateBuffer_AllocatesSlotAssignsIdentityBindsEntity()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var buffer = accessor.Create<int>(handle);
        buffer.SetComponent(PositionId, new TestPosition { X = 42f, Y = 99f });

        accessor.Playback();

        // Entity should now be Bound
        await Assert.That(world.GetEntityState(buffer.EntityId)).IsEqualTo(EntityState.Bound);

        // Component data should be readable
        var pos = storage.Get<TestPosition>(PositionId, 0);
        await Assert.That(pos.X).IsEqualTo(42f);
        await Assert.That(pos.Y).IsEqualTo(99f);
    }

    [Test]
    public async Task Playback_ModifyBuffer_UpdatesComponentValue()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Create an entity directly
        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 1f, Y = 2f });
        storage.Assign(entityId, slot);

        // Modify via command buffer
        var buffer = accessor.Modify<int>(entityId);
        buffer.SetComponent(PositionId, new TestPosition { X = 100f, Y = 200f });

        accessor.Playback();

        // Component should be updated
        var pos = storage.Get<TestPosition>(PositionId, slot);
        await Assert.That(pos.X).IsEqualTo(100f);
        await Assert.That(pos.Y).IsEqualTo(200f);
    }

    [Test]
    public async Task Playback_DestroyBuffer_RemovesEntityAndReclaimsId()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Create an entity directly
        var entityId = world.AllocateEntityId();
        var slot = storage.AllocateEntity();
        storage.Set(PositionId, slot, new TestPosition { X = 1f, Y = 2f });
        storage.Assign(entityId, slot);

        // Destroy via command buffer
        var buffer = accessor.Modify<int>(entityId);
        buffer.DestroyEntity();

        accessor.Playback();

        // Entity should be reclaimed (Null state)
        await Assert.That(world.GetEntityState(entityId)).IsEqualTo(EntityState.Null);
        await Assert.That(world.IsValid(entityId)).IsFalse();
    }

    // --- FIFO Ordering Test ---

    [Test]
    public async Task Playback_FIFO_BuffersPlayedInAllocationOrder()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Create two entities via command buffers
        var buffer1 = accessor.Create<int>(handle);
        buffer1.SetComponent(PositionId, new TestPosition { X = 1f, Y = 10f });

        var buffer2 = accessor.Create<int>(handle);
        buffer2.SetComponent(PositionId, new TestPosition { X = 2f, Y = 20f });

        accessor.Playback();

        // First buffer's entity should get slot 0, second gets slot 1 (FIFO)
        var pos0 = storage.Get<TestPosition>(PositionId, 0);
        var pos1 = storage.Get<TestPosition>(PositionId, 1);

        await Assert.That(pos0.X).IsEqualTo(1f);
        await Assert.That(pos0.Y).IsEqualTo(10f);
        await Assert.That(pos1.X).IsEqualTo(2f);
        await Assert.That(pos1.Y).IsEqualTo(20f);
    }

    // --- Auto-Clear Test ---

    [Test]
    public async Task Playback_AutoClear_AccessorEmptyAfterPlayback()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        var buffer = accessor.Create<int>(handle);
        buffer.SetComponent(PositionId, new TestPosition { X = 1f, Y = 2f });

        accessor.Playback();

        // Second playback should be a no-op (auto-cleared)
        // Create another entity to verify accessor works fresh
        var buffer2 = accessor.Create<int>(handle);
        buffer2.SetComponent(PositionId, new TestPosition { X = 99f, Y = 99f });

        accessor.Playback();

        // Should now have 2 entities total (one from each playback cycle)
        var pos0 = storage.Get<TestPosition>(PositionId, 0);
        var pos1 = storage.Get<TestPosition>(PositionId, 1);
        await Assert.That(pos0.X).IsEqualTo(1f);
        await Assert.That(pos1.X).IsEqualTo(99f);
    }

    // --- End-to-End with ComponentQuery ---

    [Test]
    public async Task EndToEnd_CreateViaCommandBuffer_VerifyWithComponentQuery()
    {
        using var world = IWorld.Create();
        var accessor = new CommandBufferAccessor(world);

        var tracker = new FakeObjectTracker();
        using var storage = CreateStorage(tracker, world, [PositionId, VelocityId]);
        var handle = world.AddStorage(storage, storage.CreateCapabilityRegistrations());
        storage.SetHandle(handle);

        // Create 3 entities via command buffer
        for (int i = 0; i < 3; i++)
        {
            var buffer = accessor.Create<int>(handle);
            buffer.SetComponent(PositionId, new TestPosition { X = i * 10f, Y = i * 20f });
            buffer.SetComponent(VelocityId, new TestVelocity { Dx = i * 1f, Dy = i * 2f });
        }

        accessor.Playback();

        // Verify via ComponentQuery
        var metadata = new ComponentQueryMetadata([PositionId, VelocityId]);
        using var query = (ComponentQuery)metadata.CreateQuery(world);

        var positions = new List<TestPosition>();
        foreach (var entity in query)
        {
            positions.Add(entity.Get<TestPosition>(PositionId));
        }

        await Assert.That(positions).HasCount().EqualTo(3);
        await Assert.That(positions[0].X).IsEqualTo(0f);
        await Assert.That(positions[1].X).IsEqualTo(10f);
        await Assert.That(positions[2].X).IsEqualTo(20f);
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
