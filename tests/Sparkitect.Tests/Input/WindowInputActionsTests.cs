using Silk.NET.Input;
using Sparkitect.Events;
using Sparkitect.Input;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.WindowInput;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.Tests.Input;

/// <summary>
/// Covers the WindowInput-hosted consume contract: registration-driven default-binding wiring,
/// every-frame push through the action's event, NoValue drop, pull preserving already-processed
/// results without resampling, disposable-is-the-lifetime-unit semantics (idempotent dispose,
/// fail-loud read after dispose), and the residual-binding teardown sweep.
/// </summary>
public class WindowInputActionsTests
{
    private sealed class StubKeyProvider : IInputSourceProvider<Key, bool>
    {
        internal bool Pressed { get; set; }

        public void Sample(ReadOnlySpan<Key> values, Span<bool> results)
        {
            for (var i = 0; i < values.Length; i++) results[i] = Pressed;
        }
    }

    /// <summary>Differentiates pressed state per key, so two actions in the same binding-type group
    /// can be told apart -- needed to prove swap-remove index retargeting on unregister. Also records
    /// the last sampled span length, the only externally observable signal of how many live binding
    /// instances the type-bunched group storage still holds.</summary>
    private sealed class SelectiveKeyProvider : IInputSourceProvider<Key, bool>
    {
        private readonly HashSet<Key> _pressed = [];

        internal int LastSampleCount { get; private set; }

        internal void Press(Key key) => _pressed.Add(key);

        public void Sample(ReadOnlySpan<Key> values, Span<bool> results)
        {
            LastSampleCount = values.Length;
            for (var i = 0; i < values.Length; i++) results[i] = _pressed.Contains(values[i]);
        }
    }

    private static (WindowInputActions Actions, ActionRegistry Registry) NewRuntime()
    {
        var settings = new SettingsManager();
        var events = new EventManager();
        var actions = new WindowInputActions { SettingsManager = settings, EventManager = events };
        var registry = new ActionRegistry(settings, events, actions);
        return (actions, registry);
    }

    private static Identification<bool> RegisterKeyAction(ActionRegistry registry, uint objectId, Key key)
    {
        var bareId = Identification.Create(720, 1, objectId);
        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(key));
        return new Identification<bool>(bareId);
    }

    [Test]
    public async Task Push_FiresCallback_OnEveryProcessedFrame_ForUnchangedValue()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 1, Key.Space);

        var callCount = 0;
        using var push = actions.Push(actionId, _ => callCount++);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        await Assert.That(callCount).IsEqualTo(2);
    }

    [Test]
    public async Task Push_NoValueFrame_InvokesNoCallback()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = false };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 2, Key.Space);

        var fired = false;
        using var push = actions.Push(actionId, _ => fired = true);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        await Assert.That(fired).IsFalse();
    }

    [Test]
    public async Task Pull_ReturnsSameProcessedResult_NoResampleBetweenReads()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 3, Key.Space);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        using var pull = actions.Pull(actionId);
        var first = pull.Read();

        // Flips the raw source AFTER the frame processed, with no intervening ProcessFrame --
        // Read() must return the already-processed value, never resample/reevaluate.
        provider.Pressed = false;
        var second = pull.Read();

        await Assert.That(first.HasValue).IsTrue();
        await Assert.That(first.Value()).IsTrue();
        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task Pull_PreservesNoValue()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = false };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 4, Key.Space);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        using var pull = actions.Pull(actionId);
        var result = pull.Read();

        await Assert.That(result.HasValue).IsFalse();
    }

    [Test]
    public async Task Push_Dispose_StopsCallbacksImmediately_AndIsIdempotent()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 5, Key.Space);

        var callCount = 0;
        var push = actions.Push(actionId, _ => callCount++);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1);

        push.Dispose();
        push.Dispose(); // idempotent -- must not throw

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task Pull_ReadAfterDispose_ThrowsTargetedException()
    {
        var (actions, _) = NewRuntime();
        var actionId = new Identification<bool>(Identification.Create(720, 1, 6));

        var pull = actions.Pull(actionId);
        pull.Dispose();
        pull.Dispose(); // idempotent -- must not throw

        await Assert.That(() => pull.Read()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task SweepResidualBindings_AutoCleans_UndisposedPushBinding_AndIsIdempotent()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 7, Key.Space);

        var callCount = 0;
        actions.Push(actionId, _ => callCount++); // intentionally left undisposed -- the residual under test

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1);

        ((IWindowInputActionsStateFacade)actions).SweepResidualBindings();

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1); // swept -- no further invocations

        // A repeat sweep is a harmless no-op.
        ((IWindowInputActionsStateFacade)actions).SweepResidualBindings();
    }

    [Test]
    public async Task SweepResidualBindings_AutoCleans_UndisposedPullBinding_AndIsIdempotent()
    {
        var (actions, _) = NewRuntime();
        var actionId = new Identification<bool>(Identification.Create(720, 1, 8));

        var pull = actions.Pull(actionId); // intentionally left undisposed -- the residual under test

        ((IWindowInputActionsStateFacade)actions).SweepResidualBindings();

        await Assert.That(() => pull.Read()).Throws<ObjectDisposedException>();

        // A repeat sweep is a harmless no-op.
        ((IWindowInputActionsStateFacade)actions).SweepResidualBindings();
    }

    [Test]
    public async Task RegisterAction_BeforeAdapterExists_StaysPending_MaterializesWhenAdapterArrives()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        // A setting shape no adapter interprets yet -- a composition state, never an error.
        var bareId = Identification.Create(720, 1, 9);
        registry.RegisterAction<bool, char>(bareId, new ActionDescription<bool, char>(' '));
        var actionId = new Identification<bool>(bareId);

        var callCount = 0;
        using var push = actions.Push(actionId, _ => callCount++);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(0); // pending -- nothing wired

        actions.RegisterBindingAdapter<char, KeyboardKey, bool>(_ => new KeyboardKey(Key.Space));

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1); // materialized on adapter arrival
    }

    [Test]
    public async Task RegisterAction_AdapterResultTypeMismatch_FailsLoud()
    {
        var (actions, registry) = NewRuntime();

        // The Key adapter produces bool; an action declaring float over a Key default is a bug.
        var bareId = Identification.Create(720, 1, 10);
        await Assert.That(() => registry.RegisterAction<float, Key>(
            bareId, new ActionDescription<float, Key>(Key.Space))).Throws<InvalidOperationException>();
    }

    // Regression: unregistering an action must compact its live instance out of the
    // type-bunched binding-group storage (not just detach it from combination), so it stops being
    // sampled/evaluated every frame and storage stays bounded across register/unregister cycles.

    [Test]
    public async Task Unregister_StopsPublishing_ForTheUnregisteredAction()
    {
        var (actions, registry) = NewRuntime();
        var provider = new StubKeyProvider { Pressed = true };
        using var source = actions.RegisterSource<Key, bool>(provider);

        var actionId = RegisterKeyAction(registry, 20, Key.Space);

        var callCount = 0;
        using var push = actions.Push(actionId, _ => callCount++);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1);

        registry.Unregister(actionId);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(callCount).IsEqualTo(1); // no further publishes once unregistered
    }

    [Test]
    public async Task Unregister_CompactsGroupStorage_DetachedInstanceNoLongerSampled()
    {
        var (actions, registry) = NewRuntime();
        var provider = new SelectiveKeyProvider();
        provider.Press(Key.W);
        provider.Press(Key.S);
        using var source = actions.RegisterSource<Key, bool>(provider);

        var firstId = RegisterKeyAction(registry, 30, Key.W);
        RegisterKeyAction(registry, 31, Key.S);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(provider.LastSampleCount).IsEqualTo(2);

        registry.Unregister(firstId);

        // The unregistered binding's instance must be compacted out of the shared group's storage --
        // not merely detached from combination -- so the sampling span shrinks and it is never
        // sampled again.
        ((IWindowInputActionsStateFacade)actions).ProcessFrame();
        await Assert.That(provider.LastSampleCount).IsEqualTo(1);
    }

    [Test]
    public async Task Unregister_EarlierAction_RetargetsSurvivingSiblingInSameGroup()
    {
        var (actions, registry) = NewRuntime();
        var provider = new SelectiveKeyProvider();
        provider.Press(Key.W);
        provider.Press(Key.S);
        using var source = actions.RegisterSource<Key, bool>(provider);

        // Both share the same type-bunched KeyboardKey group; "first" occupies the earlier slot a
        // swap-remove would reuse.
        var firstId = RegisterKeyAction(registry, 21, Key.W);
        var secondId = RegisterKeyAction(registry, 22, Key.S);

        var secondFires = 0;
        var secondValue = false;
        using var pushSecond = actions.Push(secondId, value =>
        {
            secondFires++;
            secondValue = value;
        });

        // Unregistering the earlier-added action forces a swap-remove in the shared group; the
        // surviving sibling's live index must be retargeted so it keeps resolving correctly.
        registry.Unregister(firstId);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        await Assert.That(secondFires).IsEqualTo(1);
        await Assert.That(secondValue).IsTrue();
    }

    [Test]
    public async Task Unregister_ReRegisterSameId_Oscillation_WorksWithFreshBinding()
    {
        var (actions, registry) = NewRuntime();
        var provider = new SelectiveKeyProvider();
        provider.Press(Key.W);
        using var source = actions.RegisterSource<Key, bool>(provider);

        var bareId = Identification.Create(720, 1, 23);
        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(Key.W));
        var actionId = new Identification<bool>(bareId);

        registry.Unregister(actionId);

        // Re-register the same id fresh -- must not resolve through any stale, still-sampled instance
        // left behind by the prior registration.
        registry.RegisterAction<bool, Key>(bareId, new ActionDescription<bool, Key>(Key.W));

        var fires = 0;
        using var push = actions.Push(actionId, _ => fires++);

        ((IWindowInputActionsStateFacade)actions).ProcessFrame();

        await Assert.That(fires).IsEqualTo(1);
    }

    [Test]
    public async Task Unregister_PendingActionNeverMaterialized_DoesNotThrow()
    {
        var (actions, registry) = NewRuntime();

        // A setting shape no adapter interprets yet -- LiveBinding stays null (pending).
        var bareId = Identification.Create(720, 1, 24);
        registry.RegisterAction<bool, char>(bareId, new ActionDescription<bool, char>(' '));
        var actionId = new Identification<bool>(bareId);

        await Assert.That(() => registry.Unregister(actionId)).ThrowsNothing();
    }
}
