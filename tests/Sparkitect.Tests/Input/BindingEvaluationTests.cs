using System.Numerics;
using Silk.NET.Input;
using Sparkitect.Input;
using Sparkitect.Settings;
using Sparkitect.WindowInput;

namespace Sparkitect.Tests.Input;

public class BindingEvaluationTests
{
    [Test]
    public async Task NoValue_HasValueIsFalse()
    {
        var result = ActionResult<int>.NoValue;

        await Assert.That(result.HasValue).IsFalse();
    }

    [Test]
    public async Task Value_HasValueIsTrueAndYieldsPayload()
    {
        var result = ActionResult<int>.Value(42);

        await Assert.That(result.HasValue).IsTrue();
        await Assert.That(result.Value()).IsEqualTo(42);
    }

    [Test]
    public async Task Value_OnNoValue_Throws()
    {
        var result = ActionResult<int>.NoValue;

        await Assert.That(() => result.Value()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task OrDefault_OnNoValue_ReturnsFallback()
    {
        var result = ActionResult<int>.NoValue;

        await Assert.That(result.OrDefault(-1)).IsEqualTo(-1);
    }

    [Test]
    public async Task OrDefault_OnValue_ReturnsValue_IgnoringFallback()
    {
        var result = ActionResult<int>.Value(7);

        await Assert.That(result.OrDefault(-1)).IsEqualTo(7);
    }

    [Test]
    public async Task IsValueType_ZeroAllocationShape()
    {
        await Assert.That(typeof(ActionResult<int>).IsValueType).IsTrue();
    }

    [Test]
    public async Task Equality_TwoValuesWithSamePayload_AreEqual()
    {
        var a = ActionResult<int>.Value(5);
        var b = ActionResult<int>.Value(5);

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task Equality_NoValueInstances_AreEqual()
    {
        await Assert.That(ActionResult<int>.NoValue).IsEqualTo(ActionResult<int>.NoValue);
    }

    [Test]
    public async Task Equality_ValueAndNoValue_AreNotEqual()
    {
        await Assert.That(ActionResult<int>.Value(0)).IsNotEqualTo(ActionResult<int>.NoValue);
    }

    [Test]
    public async Task PressEdge_PreviousNoValue_CurrentValueTrue_IsPressEdge()
    {
        var previous = ActionResult<bool>.NoValue;
        var current = ActionResult<bool>.Value(true);

        await Assert.That(ActionEdge.IsPressEdge(previous, current)).IsTrue();
        await Assert.That(ActionEdge.IsReleaseEdge(previous, current)).IsFalse();
    }

    [Test]
    public async Task PressEdge_HeldAcrossFrames_IsNotRepeatedPressEdge()
    {
        var previous = ActionResult<bool>.Value(true);
        var current = ActionResult<bool>.Value(true);

        await Assert.That(ActionEdge.IsPressEdge(previous, current)).IsFalse();
    }

    [Test]
    public async Task ReleaseEdge_ValueTrueToNoValue_IsReleaseEdge()
    {
        var previous = ActionResult<bool>.Value(true);
        var current = ActionResult<bool>.NoValue;

        await Assert.That(ActionEdge.IsReleaseEdge(previous, current)).IsTrue();
        await Assert.That(ActionEdge.IsPressEdge(previous, current)).IsFalse();
    }

    [Test]
    public async Task NoEdge_NoValueAcrossFrames_IsNeitherPressNorRelease()
    {
        var previous = ActionResult<bool>.NoValue;
        var current = ActionResult<bool>.NoValue;

        await Assert.That(ActionEdge.IsPressEdge(previous, current)).IsFalse();
        await Assert.That(ActionEdge.IsReleaseEdge(previous, current)).IsFalse();
    }

    [Test]
    public async Task KeyboardKey_Pressed_ComposesValueTrue()
    {
        var instances = new[] { new KeyboardKey(Key.Space, isPressed: true) };
        var results = new ActionResult<bool>[1];

        KeyboardKey.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsTrue();
    }

    [Test]
    public async Task KeyboardKey_Unpressed_ComposesNoValue_NeverValueFalse()
    {
        var instances = new[] { new KeyboardKey(Key.Space, isPressed: false) };
        var results = new ActionResult<bool>[1];

        KeyboardKey.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsFalse();
    }

    [Test]
    public async Task KeyboardAxis_PositivePressed_ComposesValuePlusOne()
    {
        var instances = new[]
        {
            new KeyboardAxis(new InputAxis<Key>(Key.A, Key.D), negativePressed: false, positivePressed: true)
        };
        var results = new ActionResult<float>[1];

        KeyboardAxis.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsEqualTo(1f);
    }

    [Test]
    public async Task KeyboardAxis_NegativePressed_ComposesValueMinusOne()
    {
        var instances = new[]
        {
            new KeyboardAxis(new InputAxis<Key>(Key.A, Key.D), negativePressed: true, positivePressed: false)
        };
        var results = new ActionResult<float>[1];

        KeyboardAxis.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsEqualTo(-1f);
    }

    [Test]
    public async Task KeyboardAxis_NeitherPressed_ComposesNoValue()
    {
        var instances = new[]
        {
            new KeyboardAxis(new InputAxis<Key>(Key.A, Key.D), negativePressed: false, positivePressed: false)
        };
        var results = new ActionResult<float>[1];

        KeyboardAxis.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsFalse();
    }

    private static readonly InputVector2<Key> WasdSetting =
        new(Up: Key.W, Down: Key.S, Left: Key.A, Right: Key.D);

    [Test]
    public async Task KeyboardVector2_WPressed_ComposesUpVector()
    {
        var instances = new[] { new KeyboardVector2(WasdSetting, upPressed: true) };
        var results = new ActionResult<Vector2>[1];

        KeyboardVector2.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsEqualTo(new Vector2(0, 1));
    }

    [Test]
    public async Task KeyboardVector2_ADPressed_CancelOnXAxis()
    {
        var instances = new[] { new KeyboardVector2(WasdSetting, leftPressed: true, rightPressed: true) };
        var results = new ActionResult<Vector2>[1];

        KeyboardVector2.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsEqualTo(Vector2.Zero);
    }

    [Test]
    public async Task KeyboardVector2_DPressed_ComposesRightVector()
    {
        var instances = new[] { new KeyboardVector2(WasdSetting, rightPressed: true) };
        var results = new ActionResult<Vector2>[1];

        KeyboardVector2.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsTrue();
        await Assert.That(results[0].Value()).IsEqualTo(new Vector2(1, 0));
    }

    [Test]
    public async Task KeyboardVector2_NoneUnpressed_ComposesNoValue_NeverValueZero()
    {
        var instances = new[] { new KeyboardVector2(WasdSetting) };
        var results = new ActionResult<Vector2>[1];

        KeyboardVector2.Evaluate(instances, results);

        await Assert.That(results[0].HasValue).IsFalse();
    }

    [Test]
    public async Task InputVector2Setting_UsableAsCompositeSettingDefinition_RoundTripsInMemory()
    {
        // Proves a struct-of-4-keys is usable as SettingDefinition<TComposite>'s value, enums
        // stored directly (no string encoding), and round-trips through the Default property
        // untouched.
        var composite = WasdSetting;
        var definition = new SettingDefinition<InputVector2<Key>>(composite);

        await Assert.That(definition.Default).IsEqualTo(composite);
        await Assert.That(definition.Default.Up).IsEqualTo(Key.W);
        await Assert.That(definition.Default.Down).IsEqualTo(Key.S);
        await Assert.That(definition.Default.Left).IsEqualTo(Key.A);
        await Assert.That(definition.Default.Right).IsEqualTo(Key.D);
    }
}
