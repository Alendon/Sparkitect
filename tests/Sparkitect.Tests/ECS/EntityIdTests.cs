using Sparkitect.ECS;

namespace Sparkitect.Tests.ECS;

public class EntityIdTests
{
    [Test]
    public async Task None_IsDefault_HasIsNoneTrue()
    {
        var none = default(EntityId);

        await Assert.That(none.IsNone).IsTrue();
        await Assert.That(EntityId.None.IsNone).IsTrue();
        await Assert.That(none).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task ValidEntityId_HasIsNoneFalse()
    {
        var id = new EntityId(0, 1);

        await Assert.That(id.IsNone).IsFalse();
    }

    [Test]
    public async Task Equality_SameIndexAndGeneration_AreEqual()
    {
        var a = new EntityId(3, 7);
        var b = new EntityId(3, 7);

        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
        await Assert.That(a != b).IsFalse();
    }

    [Test]
    public async Task Inequality_DifferentIndex_AreNotEqual()
    {
        var a = new EntityId(1, 5);
        var b = new EntityId(2, 5);

        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task Inequality_DifferentGeneration_AreNotEqual()
    {
        var a = new EntityId(1, 5);
        var b = new EntityId(1, 6);

        await Assert.That(a.Equals(b)).IsFalse();
        await Assert.That(a == b).IsFalse();
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task GetHashCode_EqualEntities_ProduceSameHash()
    {
        var a = new EntityId(10, 20);
        var b = new EntityId(10, 20);

        await Assert.That(a.GetHashCode()).IsEqualTo(b.GetHashCode());
    }

    [Test]
    public async Task ToString_ProducesExpectedFormat()
    {
        var id = new EntityId(42, 3);

        await Assert.That(id.ToString()).IsEqualTo("Entity(42:3)");
    }

    [Test]
    public async Task EqualsObject_WithMatchingStorageHandle_ReturnsFalse()
    {
        var id = new EntityId(1, 1);
        object other = new StorageHandle(1, 1);

        await Assert.That(id.Equals(other)).IsFalse();
    }

    [Test]
    public async Task EqualsObject_WithMatchingEntityId_ReturnsTrue()
    {
        var id = new EntityId(1, 1);
        object other = new EntityId(1, 1);

        await Assert.That(id.Equals(other)).IsTrue();
    }
}
