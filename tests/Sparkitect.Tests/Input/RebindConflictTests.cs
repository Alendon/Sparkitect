using System.Numerics;
using Silk.NET.Input;
using Sparkitect.Events;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Tests.Settings;
using Sparkitect.Utils.DU;
using Sparkitect.WindowInput;

namespace Sparkitect.Tests.Input;

/// <summary>
/// Covers INPT-05's headless rebind model (D-21/D-22/D-23): the AddBinding/RemoveBinding/Rebind
/// verbs round-tripping through Settings, the atomic composite-value-type rebind proof (R-6), the
/// informational (non-vetoing) conflict reverse-lookup query, and the oscillation-safe rebind-dirty
/// subscription (Pitfall 1).
/// </summary>
public class RebindConflictTests
{
    private static readonly Identification UserSourceId = Identification.Create(710, 9, 1);

    private static (SettingsManager Settings, EventManager Events, InputManager Manager) NewManager()
    {
        var settingsManager = new SettingsManager();
        settingsManager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        var eventManager = new EventManager();
        var manager = new InputManager { EventManager = eventManager, SettingsManager = settingsManager };
        return (settingsManager, eventManager, manager);
    }

    [Test]
    public async Task AddBinding_Rebind_RemoveBinding_RoundTripThroughSettings()
    {
        var (settings, _, manager) = NewManager();
        var actionId = new Identification<bool>(Identification.Create(710, 1, 1));
        var settingId = new Identification<KeyboardKeySetting>(Identification.Create(710, 2, 1));

        var handle = manager.AddBinding(actionId, settingId, new KeyboardKeySetting(Key.Space),
            s => new KeyboardKey(s, isPressed: true));

        await Assert.That(settings.GetValue(settingId).Key).IsEqualTo(Key.Space);

        var rebindResult = manager.Rebind(handle, new KeyboardKeySetting(Key.W));
        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingId).Key).IsEqualTo(Key.W);

        manager.RemoveBinding(handle);
        await Assert.That(settings.GetDeclaration(settingId)).IsNull();
    }

    [Test]
    public async Task CompositeVector2Setting_RebindsAtomically_ReadsBack()
    {
        // R-6: the codebase's first composite value-type setting rebinds in ONE atomic write, never
        // partial-field writes -- proven here at the rebind-verb layer (Plan 09 proved the settings
        // declaration itself is possible; this proves the Rebind verb round-trips it).
        var (settings, _, manager) = NewManager();
        var actionId = new Identification<Vector2>(Identification.Create(710, 1, 2));
        var settingId = new Identification<KeyboardVector2Setting>(Identification.Create(710, 2, 2));

        var initial = new KeyboardVector2Setting(Key.W, Key.S, Key.A, Key.D);
        var handle = manager.AddBinding(actionId, settingId, initial, s => new KeyboardVector2(s));

        var rebound = new KeyboardVector2Setting(Key.Up, Key.Down, Key.Left, Key.Right);
        var rebindResult = manager.Rebind(handle, rebound);

        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingId)).IsEqualTo(rebound);
    }

    [Test]
    public async Task ConflictQuery_ReturnsReverseLookup_AndRebindStillSucceeds_NoVeto()
    {
        // D-23: the reverse-lookup is informational only -- it never blocks/vetoes a rebind, even
        // when it reports a shared key across two different actions (first-match ordering already
        // resolves in-action collisions; cross-action sharing is legitimate game policy).
        var (settings, _, manager) = NewManager();
        var actionA = new Identification<bool>(Identification.Create(710, 1, 3));
        var actionB = new Identification<bool>(Identification.Create(710, 1, 4));
        var settingA = new Identification<KeyboardKeySetting>(Identification.Create(710, 2, 3));
        var settingB = new Identification<KeyboardKeySetting>(Identification.Create(710, 2, 4));

        var handleA = manager.AddBinding(actionA, settingA, new KeyboardKeySetting(Key.Space), s => new KeyboardKey(s));
        manager.AddBinding(actionB, settingB, new KeyboardKeySetting(Key.Space), s => new KeyboardKey(s));

        var sharingSpace = manager.FindBindingsReferencing<KeyboardKeySetting>(s => s.Key == Key.Space);
        await Assert.That(sharingSpace.Count).IsEqualTo(2);
        await Assert.That(sharingSpace.Contains((Identification)settingA)).IsTrue();
        await Assert.That(sharingSpace.Contains((Identification)settingB)).IsTrue();

        var onActionA = manager.FindBindingsOnActionReferencing<KeyboardKeySetting>(actionA, s => s.Key == Key.Space);
        await Assert.That(onActionA.Count).IsEqualTo(1);
        await Assert.That(onActionA[0]).IsEqualTo((Identification)settingA);

        // Rebinding the OTHER shared-key binding still succeeds -- no veto from the conflict.
        var rebindResult = manager.Rebind(handleA, new KeyboardKeySetting(Key.ShiftLeft));
        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingA).Key).IsEqualTo(Key.ShiftLeft);

        // The reverse-lookup for Key.Space now reflects only the un-rebound binding.
        var stillSharingSpace = manager.FindBindingsReferencing<KeyboardKeySetting>(s => s.Key == Key.Space);
        await Assert.That(stillSharingSpace.Count).IsEqualTo(1);
        await Assert.That(stillSharingSpace[0]).IsEqualTo((Identification)settingB);
    }

    [Test]
    public async Task Rebind_MarksDirty_NextSnapshotReflectsNewValue()
    {
        var (_, _, manager) = NewManager();
        var actionId = new Identification<bool>(Identification.Create(710, 1, 5));
        var settingId = new Identification<KeyboardKeySetting>(Identification.Create(710, 2, 5));

        // The factory's pressed-state is derived from the CURRENT setting's key -- a synthetic
        // stand-in for a live device sample, letting the test observe whether dirty-processing
        // actually re-invoked the factory with the NEW settings value (rather than merely writing
        // through Settings without touching the live binding instance).
        var handle = manager.AddBinding(actionId, settingId, new KeyboardKeySetting(Key.Space),
            s => new KeyboardKey(s, isPressed: s.Key == Key.W));
        manager.EstablishRebindDirtySubscriptions();

        var actionHandle = manager.Handle(actionId);

        manager.BuildSnapshot();
        var before = manager.Read<bool>(actionHandle);
        await Assert.That(before.HasValue).IsFalse();

        manager.Rebind(handle, new KeyboardKeySetting(Key.W));
        manager.BuildSnapshot();
        var after = manager.Read<bool>(actionHandle);

        await Assert.That(after.HasValue).IsTrue();
        await Assert.That(after.Value()).IsTrue();
    }

    [Test]
    public async Task RebindDirtySubscription_SurvivesSimulatedEnterReplay_Pitfall1Guard()
    {
        // Pitfall 1 regression guard: simulates GameStateManager.TransitionToParent's exact failure
        // mode -- a child-frame teardown sweeps subscriptions wholesale (ClearSubscriptionsForFrame),
        // then the parent's enter methods re-run (GameStateManager.cs:768-773). If the rebind-dirty
        // subscription were established anywhere OUTSIDE an [OnFrameEnterScheduling] function, this
        // sweep would silently and permanently disable rebinding for this binding: the second Rebind
        // call below would never mark it dirty, and `after` would stay NoValue.
        var (settings, _, manager) = NewManager();
        var actionId = new Identification<bool>(Identification.Create(710, 1, 6));
        var settingId = new Identification<KeyboardKeySetting>(Identification.Create(710, 2, 6));

        var frameTokenA = new object();
        settings.UseFrameTokenProvider(() => frameTokenA);

        var handle = manager.AddBinding(actionId, settingId, new KeyboardKeySetting(Key.Space),
            s => new KeyboardKey(s, isPressed: s.Key == Key.W));
        manager.EstablishRebindDirtySubscriptions();

        // Simulate the child-frame teardown sweep.
        settings.ClearSubscriptionsForFrame(frameTokenA);

        // Simulate the parent's enter-function replay establishing subscriptions again, now bound
        // to a new frame token -- exactly what InputModule.EstablishRebindDirtySubscriptions does
        // on every [OnFrameEnterScheduling] run, unconditionally.
        var frameTokenB = new object();
        settings.UseFrameTokenProvider(() => frameTokenB);
        manager.EstablishRebindDirtySubscriptions();

        manager.Rebind(handle, new KeyboardKeySetting(Key.W));
        manager.BuildSnapshot();

        var actionHandle = manager.Handle(actionId);
        var after = manager.Read<bool>(actionHandle);

        await Assert.That(after.HasValue).IsTrue();
        await Assert.That(after.Value()).IsTrue();
    }
}
