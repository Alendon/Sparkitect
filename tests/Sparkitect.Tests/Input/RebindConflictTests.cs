using System.Numerics;
using Silk.NET.Input;
using Sparkitect.Events;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Tests.Settings;
using Sparkitect.Utils.DU;
using Sparkitect.WindowInput;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.Tests.Input;

/// <summary>
/// Covers the Settings-backed rebind model: an action's registered default binding round-tripping
/// through Settings via the Rebind verb, the atomic composite-value-type rebind proof, the
/// informational (non-vetoing) conflict reverse-lookup query, and the oscillation-safe
/// rebind-dirty subscription.
/// </summary>
public class RebindConflictTests
{
    private static readonly Identification UserSourceId = Identification.Create(710, 9, 1);

    private sealed class StubKeyProvider : IInputSourceProvider<Key, bool>
    {
        internal Key PressedKey { get; set; } = Key.Unknown;

        public void Sample(ReadOnlySpan<Key> values, Span<bool> results)
        {
            for (var i = 0; i < values.Length; i++) results[i] = values[i] == PressedKey;
        }
    }

    private static (SettingsManager Settings, WindowInputActions Actions, ActionRegistry Registry, StubKeyProvider Provider)
        NewRuntime()
    {
        var settings = new SettingsManager();
        settings.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        var events = new EventManager();
        var actions = new WindowInputActions { SettingsManager = settings, EventManager = events };
        var registry = new ActionRegistry(settings, events, actions);
        var provider = new StubKeyProvider();
        actions.RegisterSource<Key, bool>(provider);
        return (settings, actions, registry, provider);
    }

    [Test]
    public async Task RegisterAction_Rebind_Unregister_RoundTripThroughSettings()
    {
        var (settings, actions, registry, _) = NewRuntime();
        var bareId = Identification.Create(710, 1, 1);
        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(Key.Space));
        var settingId = new Identification<Key>(bareId);

        await Assert.That(settings.GetValue(settingId)).IsEqualTo(Key.Space);

        var rebindResult = actions.Rebind(settingId, Key.W);
        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingId)).IsEqualTo(Key.W);

        registry.Unregister(bareId);
        await Assert.That(settings.GetDeclaration(settingId)).IsNull();
    }

    [Test]
    public async Task CompositeVector2Setting_RebindsAtomically_ReadsBack()
    {
        // A composite value-type default binding rebinds in ONE atomic write, never
        // partial-field writes -- proven here at the Rebind-verb layer.
        var (settings, actions, registry, _) = NewRuntime();
        var bareId = Identification.Create(710, 1, 2);
        var initial = new InputVector2<Key>(Key.W, Key.S, Key.A, Key.D);
        registry.RegisterAction<Vector2, InputVector2<Key>>(
            bareId, new ActionDescription<Vector2, InputVector2<Key>>(initial));
        var settingId = new Identification<InputVector2<Key>>(bareId);

        var rebound = new InputVector2<Key>(Key.Up, Key.Down, Key.Left, Key.Right);
        var rebindResult = actions.Rebind(settingId, rebound);

        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingId)).IsEqualTo(rebound);
    }

    [Test]
    public async Task ConflictQuery_ReturnsReverseLookup_AndRebindStillSucceeds_NoVeto()
    {
        // The reverse-lookup is informational only -- it never blocks/vetoes a rebind, even when
        // it reports a shared key across two different actions (first-match ordering already
        // resolves in-action collisions; cross-action sharing is legitimate game policy).
        var (settings, actions, registry, _) = NewRuntime();
        var actionA = Identification.Create(710, 1, 3);
        var actionB = Identification.Create(710, 1, 4);
        registry.RegisterAction<bool, Key>(actionA, new ActionDescription<bool, Key>(Key.Space));
        registry.RegisterAction<bool, Key>(actionB, new ActionDescription<bool, Key>(Key.Space));
        var settingA = new Identification<Key>(actionA);

        var sharingSpace = actions.FindBindingsReferencing<Key>(key => key == Key.Space);
        await Assert.That(sharingSpace.Count).IsEqualTo(2);
        await Assert.That(sharingSpace.Contains(actionA)).IsTrue();
        await Assert.That(sharingSpace.Contains(actionB)).IsTrue();

        var onActionA = actions.FindBindingsOnActionReferencing<Key>(actionA, key => key == Key.Space);
        await Assert.That(onActionA.Count).IsEqualTo(1);
        await Assert.That(onActionA[0]).IsEqualTo(actionA);

        // Rebinding one of the shared-key bindings still succeeds -- no veto from the conflict.
        var rebindResult = actions.Rebind(settingA, Key.ShiftLeft);
        await Assert.That(rebindResult is Result<SetError>.Ok).IsTrue();
        await Assert.That(settings.GetValue(settingA)).IsEqualTo(Key.ShiftLeft);

        // The reverse-lookup for Key.Space now reflects only the un-rebound binding.
        var stillSharingSpace = actions.FindBindingsReferencing<Key>(key => key == Key.Space);
        await Assert.That(stillSharingSpace.Count).IsEqualTo(1);
        await Assert.That(stillSharingSpace[0]).IsEqualTo(actionB);
    }

    [Test]
    public async Task Rebind_MarksDirty_NextFrameReflectsNewValue()
    {
        var (_, actions, registry, provider) = NewRuntime();
        provider.PressedKey = Key.W;

        var bareId = Identification.Create(710, 1, 5);
        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(Key.Space));
        var actionId = new Identification<bool>(bareId);
        var settingId = new Identification<Key>(bareId);

        actions.EstablishRebindDirtySubscriptions();

        using var pull = actions.Pull(actionId);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        var before = pull.Read();
        await Assert.That(before.HasValue).IsFalse();

        actions.Rebind(settingId, Key.W);
        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        var after = pull.Read();

        await Assert.That(after.HasValue).IsTrue();
        await Assert.That(after.Value()).IsTrue();
    }

    [Test]
    public async Task RebindDirtySubscription_SurvivesSimulatedEnterReplay()
    {
        // Regression guard: simulates GameStateManager.TransitionToParent's exact failure mode --
        // a child-frame teardown sweeps subscriptions wholesale (ClearSubscriptionsForFrame), then
        // the parent's enter methods re-run. If the rebind-dirty subscription were established
        // anywhere OUTSIDE an [OnFrameEnterScheduling] function, this sweep would silently and
        // permanently disable rebinding for this binding: the Rebind call below would never mark
        // it dirty, and `after` would stay NoValue.
        var (settings, actions, registry, provider) = NewRuntime();
        provider.PressedKey = Key.W;

        var bareId = Identification.Create(710, 1, 6);
        var frameTokenA = new object();
        settings.UseFrameTokenProvider(() => frameTokenA);

        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(Key.Space));
        var actionId = new Identification<bool>(bareId);
        var settingId = new Identification<Key>(bareId);

        actions.EstablishRebindDirtySubscriptions();

        // Simulate the child-frame teardown sweep.
        settings.ClearSubscriptionsForFrame(frameTokenA);

        // Simulate the parent's enter-function replay establishing subscriptions again, now bound
        // to a new frame token -- exactly what WindowInputModule's
        // establish_rebind_dirty_subscriptions transition does on every [OnFrameEnterScheduling]
        // run, unconditionally.
        var frameTokenB = new object();
        settings.UseFrameTokenProvider(() => frameTokenB);
        actions.EstablishRebindDirtySubscriptions();

        actions.Rebind(settingId, Key.W);

        using var pull = actions.Pull(actionId);
        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        var after = pull.Read();

        await Assert.That(after.HasValue).IsTrue();
        await Assert.That(after.Value()).IsTrue();
    }
}
