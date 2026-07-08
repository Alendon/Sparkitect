using Sparkitect.Events;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Events;

public class EventLifecycleTests
{
    [Test]
    public async Task Unregister_ClearsAllSubscribersForEvent()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(2, 1, 1));
        var count = 0;

        manager.Subscribe(id, _ => count++);
        manager.Subscribe(id, _ => count++);

        manager.Unregister(id);

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Unregister_DoesNotAffectOtherEvents()
    {
        var manager = new EventManager();
        var idA = new Identification<int>(Identification.Create(2, 2, 1));
        var idB = new Identification<int>(Identification.Create(2, 2, 2));
        var bCount = 0;

        manager.Subscribe(idA, _ => { });
        manager.Subscribe(idB, _ => bCount++);

        manager.Unregister(idA);

        manager.Publish(idB, 42);

        await Assert.That(bCount).IsEqualTo(1);
    }

    [Test]
    public async Task Unregister_RemovesDefinition()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(2, 3, 1));
        var count = 0;

        manager.RegisterEvent<int>(id, new EventDefinition<int>());
        manager.Unregister(id);
        manager.RegisterEvent<int>(id, new EventDefinition<int>());

        manager.Subscribe(id, _ => count++);
        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task DisposePrimaryControl_StillWorksWithoutUnregister()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(2, 4, 1));
        var count = 0;

        var binding = manager.Subscribe(id, _ => count++);
        binding.Dispose();

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(0);

        manager.Unregister(id);

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Unregister_OnEventWithNoSubscribers_IsNoOp()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(2, 5, 1));
        var count = 0;

        manager.RegisterEvent<int>(id, new EventDefinition<int>());

        manager.Unregister(id);

        manager.Subscribe(id, _ => count++);
        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(1);
    }
}
