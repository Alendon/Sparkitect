using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Components;
using Sparkitect.Modding;

namespace Sparkitect.Tests.ECS;

public struct TestPosition : IHasIdentification
{
    public float X;
    public float Y;

    public static Identification Identification { get; } = Identification.Create(1, 1, 1);
}

public struct TestVelocity : IHasIdentification
{
    public float Dx;
    public float Dy;

    public static Identification Identification { get; } = Identification.Create(1, 1, 2);
}

public class ComponentManagerTests
{
    [Test]
    public async Task Register_StoresSize()
    {
        var manager = new ComponentManager();

        manager.Register<TestPosition>(TestPosition.Identification);

        var size = manager.GetSize(TestPosition.Identification);
        await Assert.That(size).IsEqualTo(8); // 2 floats = 8 bytes
    }

    [Test]
    public async Task GetSize_UnregisteredId_Throws()
    {
        var manager = new ComponentManager();

        await Assert.That(() =>
        {
            _ = manager.GetSize(TestPosition.Identification);
        }).Throws<KeyNotFoundException>();
    }

    [Test]
    public async Task IsRegistered_ReturnsTrueAfterRegister()
    {
        var manager = new ComponentManager();

        manager.Register<TestPosition>(TestPosition.Identification);

        await Assert.That(manager.IsRegistered(TestPosition.Identification)).IsTrue();
    }

    [Test]
    public async Task IsRegistered_ReturnsFalseForUnknown()
    {
        var manager = new ComponentManager();

        await Assert.That(manager.IsRegistered(TestPosition.Identification)).IsFalse();
    }

    [Test]
    public async Task Register_DuplicateId_Throws()
    {
        var manager = new ComponentManager();

        manager.Register<TestPosition>(TestPosition.Identification);

        await Assert.That(() =>
        {
            manager.Register<TestPosition>(TestPosition.Identification);
        }).Throws<ArgumentException>();
    }
}

public class ComponentSetMetadataTests
{
    [Test]
    public async Task CarriesIdentificationSet()
    {
        var components = new HashSet<Identification>
        {
            TestPosition.Identification,
            TestVelocity.Identification
        };

        var metadata = new ComponentSetMetadata(components);

        await Assert.That(metadata.Components).HasCount().EqualTo(2);
        await Assert.That(metadata.Components.Contains(TestPosition.Identification)).IsTrue();
        await Assert.That(metadata.Components.Contains(TestVelocity.Identification)).IsTrue();
    }

    [Test]
    public async Task ImplementsICapabilityMetadata()
    {
        var metadata = new ComponentSetMetadata(new HashSet<Identification>());

        await Assert.That(metadata).IsAssignableTo<ICapabilityMetadata>();
    }
}
