using Sparkitect.Events;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Events;

public class EventManagerTests
{
    // Compile-time proof: manager.Subscribe(intId, (WrongPayload p) => {}) does not compile because intId is Identification<int>.

    [Test]
    public async Task Subscribe_ReturnsEventBinding()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 1));

        var binding = manager.Subscribe(id, _ => { });

        await Assert.That(binding).IsNotEqualTo(default(EventBinding));
        binding.Dispose();
    }

    [Test]
    public async Task Publish_DispatchesToAllSubscribersSynchronously()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 2));
        var count = 0;
        var order = new List<int>();

        manager.Subscribe(id, _ => { count++; order.Add(1); });
        manager.Subscribe(id, _ => { count++; order.Add(2); });

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(2);
        await Assert.That(order[0]).IsEqualTo(1);
        await Assert.That(order[1]).IsEqualTo(2);
    }

    [Test]
    public async Task Publish_WithNoSubscribers_IsNoOp()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 3));

        manager.Publish(id, 42);

        var count = 0;
        manager.Subscribe(id, _ => count++);
        manager.Publish(id, 7);

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_RemovesSubscription()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 4));
        var count = 0;

        var binding = manager.Subscribe(id, _ => count++);
        binding.Dispose();

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Dispose_CalledTwice_IsIdempotent()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 5));
        var count = 0;

        var binding = manager.Subscribe(id, _ => count++);
        binding.Dispose();
        binding.Dispose();

        manager.Publish(id, 42);

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task StaleBinding_AfterSlotShifted_IsNoOp()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 6));
        var bCount = 0;

        var bindingA = manager.Subscribe(id, _ => { });
        manager.Subscribe(id, _ => bCount++);

        bindingA.Dispose();

        bindingA.Dispose();

        manager.Publish(id, 42);

        await Assert.That(bCount).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_NonLastSubscriber_LaterSiblingsStillDeliveredOnPublish()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 9));
        var secondCount = 0;
        var thirdCount = 0;

        var first = manager.Subscribe(id, _ => { });
        manager.Subscribe(id, _ => secondCount++);
        manager.Subscribe(id, _ => thirdCount++);

        first.Dispose();

        manager.Publish(id, 42);

        await Assert.That(secondCount).IsEqualTo(1);
        await Assert.That(thirdCount).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_LaterSubscribersAfterEarlierRemoval_RemainIndependentlyReleasable()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 10));
        var firstCount = 0;
        var secondCount = 0;
        var thirdCount = 0;

        var first = manager.Subscribe(id, _ => firstCount++);
        var second = manager.Subscribe(id, _ => secondCount++);
        var third = manager.Subscribe(id, _ => thirdCount++);

        first.Dispose();
        second.Dispose();

        manager.Publish(id, 1);
        await Assert.That(firstCount).IsEqualTo(0);
        await Assert.That(secondCount).IsEqualTo(0);
        await Assert.That(thirdCount).IsEqualTo(1);

        third.Dispose();

        manager.Publish(id, 2);
        await Assert.That(thirdCount).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_AlreadyDisposedNonLastSubscriber_IsNoOpAndSiblingsUnaffected()
    {
        var manager = new EventManager();
        var id = new Identification<int>(Identification.Create(1, 1, 11));
        var secondCount = 0;

        var first = manager.Subscribe(id, _ => { });
        manager.Subscribe(id, _ => secondCount++);

        first.Dispose();
        first.Dispose();

        manager.Publish(id, 42);

        await Assert.That(secondCount).IsEqualTo(1);
    }

    public record struct IntPayload(int Value);

    [Test]
    public async Task StructPayload_DispatchesByValue()
    {
        var manager = new EventManager();
        var id = new Identification<IntPayload>(Identification.Create(1, 1, 7));
        IntPayload received = default;

        manager.Subscribe(id, p => received = p);

        manager.Publish(id, new IntPayload(99));

        await Assert.That(received.Value).IsEqualTo(99);
    }

    public sealed class MutablePayload
    {
        public int Value { get; set; }
    }

    [Test]
    public async Task ClassPayload_MutationsVisibleToPublisher()
    {
        var manager = new EventManager();
        var id = new Identification<MutablePayload>(Identification.Create(1, 1, 8));
        var payload = new MutablePayload { Value = 0 };

        manager.Subscribe(id, p => p.Value = 42);

        manager.Publish(id, payload);

        await Assert.That(payload.Value).IsEqualTo(42);
    }
}
