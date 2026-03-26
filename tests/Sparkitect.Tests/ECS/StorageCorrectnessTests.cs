using Sparkitect.ECS;
using Sparkitect.ECS.Storage;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public class StorageCorrectnessTests
{
    // --- EntityIdentityMap guards ---

    [Test]
    public async Task Unassign_OutOfBoundsIndex_ThrowsInvalidOperationException()
    {
        var map = new EntityIdentityMap<int>(initialCapacity: 4);
        var entityId = new EntityId(99, 1); // index 99, well beyond capacity 4

        await Assert.That(() => map.Unassign(entityId))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Unassign_NotAssigned_ThrowsInvalidOperationException()
    {
        var map = new EntityIdentityMap<int>(initialCapacity: 64);
        var entityId = new EntityId(0, 1); // index 0, within bounds but never assigned

        await Assert.That(() => map.Unassign(entityId))
            .Throws<InvalidOperationException>();
    }

    // --- NativeColumn disposed guard ---

    [Test]
    public async Task NativeColumn_Get_AfterDispose_ThrowsObjectDisposedException()
    {
        var tracker = new FakeObjectTracker();
        var column = new NativeColumn(sizeof(float), 4, tracker);
        column.AddSlot();
        column.Dispose();

        await Assert.That(() => { column.Get<float>(0); })
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task NativeColumn_Set_AfterDispose_ThrowsObjectDisposedException()
    {
        var tracker = new FakeObjectTracker();
        var column = new NativeColumn(sizeof(float), 4, tracker);
        column.AddSlot();
        column.Dispose();

        await Assert.That(() => { column.Set(0, 42f); })
            .Throws<ObjectDisposedException>();
    }

    // --- SoAStorage RemoveEntity bounds ---

    [Test]
    public async Task RemoveEntity_NegativeKey_ThrowsArgumentOutOfRangeException()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        var componentMeta = new List<(Identification Id, int Size)>
        {
            (Identification.Create(1, 1, 1), sizeof(float) * 2)
        };
        using var storage = new SoAStorage(componentMeta, tracker, world);

        await Assert.That(() => storage.RemoveEntity(-1))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RemoveEntity_KeyEqualToCount_ThrowsArgumentOutOfRangeException()
    {
        using var world = IWorld.Create();
        var tracker = new FakeObjectTracker();
        var componentMeta = new List<(Identification Id, int Size)>
        {
            (Identification.Create(1, 1, 1), sizeof(float) * 2)
        };
        using var storage = new SoAStorage(componentMeta, tracker, world);
        storage.AllocateEntity(); // count = 1

        await Assert.That(() => storage.RemoveEntity(1)) // key == count
            .Throws<ArgumentOutOfRangeException>();
    }
}
