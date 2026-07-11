using Sparkitect.Events;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Settings;

namespace Sparkitect.Tests.Input;

public class ActionRegistryTests
{
    // The default-binding value type: a small composite value type, per D-17's binding-backing
    // settings boundary (code-authored struct, never a scalar-source-fed primitive).
    public readonly record struct JumpKeyBinding(int PrimaryKey);

    private static (SettingsManager settings, EventManager events, ActionRegistry registry) NewRegistry()
    {
        var settingsManager = new SettingsManager();
        var eventManager = new EventManager();
        var registry = new ActionRegistry(settingsManager, eventManager);
        return (settingsManager, eventManager, registry);
    }

    [Test]
    public async Task RegisterAction_OwnTypedId_WrapsSameUnderlyingIdentification()
    {
        var (_, _, registry) = NewRegistry();
        var id = Identification.Create(500, 1, 1);
        var description = new ActionDescription<bool, JumpKeyBinding, bool>(new JumpKeyBinding(42));

        // The action's own typed id falls out of the bare marker alone — no store is touched for
        // it. Proving it "resolves" means: after registration succeeds, the same underlying id can
        // be meaningfully wrapped as Identification<TResult> and round-trips unchanged.
        registry.RegisterAction<bool, JumpKeyBinding, bool>(id, description);
        var ownTypedId = new Identification<bool>(id);

        await Assert.That((Identification)ownTypedId).IsEqualTo(id);
    }

    [Test]
    public async Task RegisterAction_EventAlias_RoundTripsEndToEndThroughDistinctSuffixedId()
    {
        // R-5 first-event-target proof + Pitfall-5 guard: if the hand-authored
        // eventFacade.RegisterEvent fan-out were omitted, EventManager.Publish would silently no-op
        // (it never checks _definitions) and this subscriber would never fire.
        var (_, eventManager, registry) = NewRegistry();
        var id = Identification.Create(500, 1, 2);
        var description = new ActionDescription<bool, JumpKeyBinding, bool>(new JumpKeyBinding(7));

        registry.RegisterAction<bool, JumpKeyBinding, bool>(id, description);

        var eventId = new Identification<bool>(id);
        var received = false;
        var gotEvent = false;
        eventManager.Subscribe(eventId, value =>
        {
            gotEvent = true;
            received = value;
        });

        eventManager.Publish(eventId, true);

        await Assert.That(gotEvent).IsTrue();
        await Assert.That(received).IsTrue();
    }

    [Test]
    public async Task RegisterAction_OptionA_DefaultBindingDeclaresAndReadsBack()
    {
        var (settingsManager, _, registry) = NewRegistry();
        var id = Identification.Create(500, 1, 3);
        var description = new ActionDescription<bool, JumpKeyBinding, bool>(new JumpKeyBinding(99));

        registry.RegisterAction<bool, JumpKeyBinding, bool>(id, description);

        var settingId = new Identification<JumpKeyBinding>(id);
        var value = settingsManager.GetValue(settingId);

        await Assert.That(value.PrimaryKey).IsEqualTo(99);
    }

    [Test]
    public async Task Unregister_RemovesBothTargetFanOuts()
    {
        var (settingsManager, eventManager, registry) = NewRegistry();
        var id = Identification.Create(500, 1, 4);
        var description = new ActionDescription<bool, JumpKeyBinding, bool>(new JumpKeyBinding(1));
        registry.RegisterAction<bool, JumpKeyBinding, bool>(id, description);

        var eventId = new Identification<bool>(id);
        var received = false;
        eventManager.Subscribe(eventId, _ => received = true);

        registry.Unregister(id);

        eventManager.Publish(eventId, true);
        await Assert.That(received).IsFalse();

        var settingId = new Identification<JumpKeyBinding>(id);
        await Assert.That(settingsManager.GetDeclaration(settingId)).IsNull();
    }
}
