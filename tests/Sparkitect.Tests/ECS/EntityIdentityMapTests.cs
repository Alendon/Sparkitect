using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;

namespace Sparkitect.Tests.ECS;

public class EntityIdentityMapTests
{
    [Test]
    public async Task Assign_ThenTryResolve_ReturnsKey()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(1, 1);

        map.Assign(id, 42);
        var found = map.TryResolve(id, out int key);

        await Assert.That(found).IsTrue();
        await Assert.That(key).IsEqualTo(42);
    }

    [Test]
    public async Task TryResolve_UnassignedEntityId_ReturnsFalse()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(5, 1);

        var found = map.TryResolve(id, out _);

        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task GetEntityId_ReturnsEntityIdForKey()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(1, 1);

        map.Assign(id, 7);
        var result = map.GetEntityId(7);

        await Assert.That(result).IsEqualTo(id);
    }

    [Test]
    public async Task Unassign_RemovesMapping()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(1, 1);

        map.Assign(id, 42);
        map.Unassign(id);

        var found = map.TryResolve(id, out _);
        await Assert.That(found).IsFalse();
    }

    [Test]
    public async Task NotifySwap_UpdatesReverseMapping()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(1, 1);

        map.Assign(id, 5);
        map.NotifySwap(5, 0); // Entity moves from slot 5 to slot 0

        var found = map.TryResolve(id, out int key);
        await Assert.That(found).IsTrue();
        await Assert.That(key).IsEqualTo(0);
    }

    [Test]
    public async Task NotifySwap_PreservesForwardMapping()
    {
        var map = new EntityIdentityMap<int>();
        var id = new EntityId(1, 1);

        map.Assign(id, 5);
        map.NotifySwap(5, 0);

        // Forward: EntityId.Index -> TKey should be updated to new key
        var entityId = map.GetEntityId(0);
        await Assert.That(entityId).IsEqualTo(id);
    }

    [Test]
    public async Task Assign_GrowsForwardArrays_WhenEntityIdIndexExceedsCapacity()
    {
        var map = new EntityIdentityMap<int>(initialCapacity: 4);
        var id = new EntityId(100, 1); // Way past initial capacity

        map.Assign(id, 42);
        var found = map.TryResolve(id, out int key);

        await Assert.That(found).IsTrue();
        await Assert.That(key).IsEqualTo(42);
    }

    [Test]
    public async Task MultipleEntities_TrackedSimultaneously()
    {
        var map = new EntityIdentityMap<int>();
        var id1 = new EntityId(1, 1);
        var id2 = new EntityId(2, 1);
        var id3 = new EntityId(3, 1);

        map.Assign(id1, 0);
        map.Assign(id2, 1);
        map.Assign(id3, 2);

        var found1 = map.TryResolve(id1, out int key1);
        var found2 = map.TryResolve(id2, out int key2);
        var found3 = map.TryResolve(id3, out int key3);

        await Assert.That(found1).IsTrue();
        await Assert.That(key1).IsEqualTo(0);
        await Assert.That(found2).IsTrue();
        await Assert.That(key2).IsEqualTo(1);
        await Assert.That(found3).IsTrue();
        await Assert.That(key3).IsEqualTo(2);
    }

    [Test]
    public async Task GetEntityId_UnmappedKey_ThrowsKeyNotFoundException()
    {
        var map = new EntityIdentityMap<int>();

        await Assert.That(() =>
        {
            _ = map.GetEntityId(999);
        }).Throws<KeyNotFoundException>();
    }
}
