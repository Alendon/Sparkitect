using Silk.NET.Input;
using Sparkitect.Events;
using Sparkitect.Input;
using Sparkitect.Input.Bindings;
using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.WindowInput;

namespace Sparkitect.Tests.Input;

public class InputSnapshotTests
{
    private static InputManager NewManager(out EventManager eventManager)
    {
        eventManager = new EventManager();
        // SettingsManager is unused by this file's tests (no AddBinding/Rebind calls here) -- Plan
        // 08 added it as a required InputManager dependency for the rebind verbs.
        return new InputManager { EventManager = eventManager, SettingsManager = new SettingsManager() };
    }

    [Test]
    public async Task Poll_And_Edge_Agree_From_One_Snapshot()
    {
        var manager = NewManager(out var eventManager);
        var id = Identification.Create(600, 1, 1);
        var actionId = new Identification<bool>(id);
        var handle = manager.Handle(actionId);

        manager.RegisterBinding(actionId, new KeyboardKey(new KeyboardKeySetting(Key.Space), isPressed: true));

        bool? published = null;
        eventManager.Subscribe(actionId, value => published = value);

        manager.BuildSnapshot();

        var polled = manager.Read<bool>(handle);

        await Assert.That(polled.HasValue).IsTrue();
        await Assert.That(polled.Value()).IsTrue();
        await Assert.That(published).IsNotNull();
        await Assert.That(published!.Value).IsEqualTo(polled.Value());
    }

    [Test]
    public async Task NonContributing_Action_Polls_NoValue_And_Fires_No_Edge()
    {
        var manager = NewManager(out var eventManager);
        var id = Identification.Create(600, 1, 2);
        var actionId = new Identification<bool>(id);
        var handle = manager.Handle(actionId);

        manager.RegisterBinding(actionId, new KeyboardKey(new KeyboardKeySetting(Key.Space), isPressed: false));

        var fired = false;
        eventManager.Subscribe(actionId, _ => fired = true);

        manager.BuildSnapshot();

        var polled = manager.Read<bool>(handle);

        await Assert.That(polled.HasValue).IsFalse();
        await Assert.That(fired).IsFalse();
    }

    [Test]
    public async Task FirstMatchWins_Across_Two_Bindings_On_One_Action_NoValue_Binding_Is_Skipped()
    {
        var manager = NewManager(out _);
        var id = Identification.Create(600, 1, 3);
        var actionId = new Identification<bool>(id);
        var handle = manager.Handle(actionId);

        // Stored order: first binding is unpressed (NoValue, skipped), second is pressed (wins).
        manager.RegisterBinding(actionId, new KeyboardKey(new KeyboardKeySetting(Key.W), isPressed: false));
        manager.RegisterBinding(actionId, new KeyboardKey(new KeyboardKeySetting(Key.S), isPressed: true));

        manager.BuildSnapshot();

        var polled = manager.Read<bool>(handle);

        await Assert.That(polled.HasValue).IsTrue();
        await Assert.That(polled.Value()).IsTrue();
    }

    [Test]
    public async Task FirstMatchWins_TheFIRST_Contributing_Binding_Wins_Not_The_Last()
    {
        var manager = NewManager(out _);
        var id = Identification.Create(600, 1, 4);
        var actionId = new Identification<int>(id);
        var handle = manager.Handle(actionId);

        // All three contribute a value; stored order says the FIRST one wins, not the last.
        manager.RegisterBinding(actionId, new FirstMatchProbe(1));
        manager.RegisterBinding(actionId, new FirstMatchProbe(2));
        manager.RegisterBinding(actionId, new FirstMatchProbe(3));

        manager.BuildSnapshot();

        var polled = manager.Read<int>(handle);

        await Assert.That(polled.HasValue).IsTrue();
        await Assert.That(polled.Value()).IsEqualTo(1);
    }

    [Test]
    [NotInParallel(nameof(CountingBinding))]
    public async Task Evaluation_Is_Bunched_By_Concrete_Type_One_Call_For_Multiple_Instances()
    {
        CountingBinding.EvaluateCallCount = 0;

        var manager = NewManager(out _);
        var actionA = new Identification<bool>(Identification.Create(600, 1, 5));
        var actionB = new Identification<bool>(Identification.Create(600, 1, 6));

        // Three instances of the SAME concrete binding type, spread across two different actions --
        // D-18 requires exactly one Evaluate() call for the whole type-group, not one per instance.
        manager.RegisterBinding(actionA, new CountingBinding(true));
        manager.RegisterBinding(actionA, new CountingBinding(false));
        manager.RegisterBinding(actionB, new CountingBinding(true));

        manager.BuildSnapshot();

        await Assert.That(CountingBinding.EvaluateCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task ProviderlessChannel_Is_Default_Quiet_Never_An_Error()
    {
        var manager = NewManager(out _);
        var id = Identification.Create(600, 1, 7);
        var actionId = new Identification<bool>(id);
        var handle = manager.Handle(actionId);

        // No RegisterSource call for the keyboard channel was ever made -- D-20: a providerless
        // channel is a composition state, never an error. The binding's own already-sampled state
        // (unpressed) simply composes to NoValue, and nothing throws.
        manager.RegisterBinding(actionId, new KeyboardKey(new KeyboardKeySetting(Key.Space)));

        manager.BuildSnapshot();

        var polled = manager.Read<bool>(handle);
        await Assert.That(polled.HasValue).IsFalse();
    }

    [Test]
    public async Task RegisterSource_Dispose_Unregisters_And_Is_Idempotent()
    {
        var manager = NewManager(out _);
        var provider = new FakeSourceProvider();

        var binding = manager.RegisterSource<int, bool>(provider);
        binding.Dispose();

        // Disposing twice must not throw -- mirrors EventBinding's disposal idiom.
        binding.Dispose();
    }

    private sealed class FakeSourceProvider : IInputSourceProvider<int, bool>
    {
        public void Sample(ReadOnlySpan<int> values, Span<bool> results)
        {
            for (var i = 0; i < values.Length; i++)
            {
                results[i] = false;
            }
        }
    }

    /// <summary>
    /// Test-only binding type proving D-19's "first contributing binding in stored order wins" with
    /// distinguishable payloads (unlike <see cref="KeyboardKey"/>'s bool result, where two contributing
    /// instances are indistinguishable).
    /// </summary>
    private readonly struct FirstMatchProbe : IBindingType<FirstMatchProbe, int>
    {
        private readonly int _value;

        public FirstMatchProbe(int value) => _value = value;

        public static void Evaluate(ReadOnlySpan<FirstMatchProbe> instances, Span<ActionResult<int>> results)
        {
            for (var i = 0; i < instances.Length; i++)
            {
                results[i] = ActionResult<int>.Value(instances[i]._value);
            }
        }
    }

    /// <summary>
    /// Test-only binding type proving type-bunched evaluation (D-18): <see cref="Evaluate"/> increments
    /// a shared counter exactly once per call, regardless of how many instances the span carries.
    /// </summary>
    private readonly struct CountingBinding : IBindingType<CountingBinding, bool>
    {
        internal static int EvaluateCallCount;

        private readonly bool _isPressed;

        public CountingBinding(bool isPressed) => _isPressed = isPressed;

        public static void Evaluate(ReadOnlySpan<CountingBinding> instances, Span<ActionResult<bool>> results)
        {
            EvaluateCallCount++;
            for (var i = 0; i < instances.Length; i++)
            {
                results[i] = instances[i]._isPressed ? ActionResult<bool>.Value(true) : ActionResult<bool>.NoValue;
            }
        }
    }
}
