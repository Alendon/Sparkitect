using JetBrains.Annotations;
using Silk.NET.Input;
using Sparkitect.Input;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.WindowInput;

/// <summary>
/// An analog-axis keyboard binding type: composes two keys' sampled pressed-states into an
/// <see cref="ActionResult{T}"/> of <see cref="float"/> along a -1..+1 axis. Neither key pressed
/// produces <see cref="ActionResult{T}.NoValue"/>, NEVER <c>Value(0f)</c> — "produces a value"
/// means "actively contributing"; a pressed key produces <c>Value(-1f)</c> or <c>Value(+1f)</c>.
/// When both keys are pressed simultaneously, the positive extreme wins (arbitrary but
/// deterministic tie-break).
/// </summary>
/// <remarks>
/// <see cref="NegativePressed"/>/<see cref="PositivePressed"/> are the raw sampled channel slots
/// for this instance's <see cref="Setting"/>'s two keys, written by <see cref="Sample"/> before
/// <see cref="Evaluate"/> runs — this type never samples the device itself.
/// </remarks>
[PublicAPI]
public readonly struct KeyboardAxis : IBindingType<KeyboardAxis, float>
{
    /// <summary>The binding-backing setting: which two keys drive this axis.</summary>
    public InputAxis<Key> Setting { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s negative key.</summary>
    public bool NegativePressed { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s positive key.</summary>
    public bool PositivePressed { get; }

    /// <summary>Creates a keyboard-axis binding instance.</summary>
    /// <param name="setting">The two keys driving this axis.</param>
    /// <param name="negativePressed">The current frame's sampled pressed-state for the negative key.</param>
    /// <param name="positivePressed">The current frame's sampled pressed-state for the positive key.</param>
    public KeyboardAxis(InputAxis<Key> setting, bool negativePressed = false, bool positivePressed = false)
    {
        Setting = setting;
        NegativePressed = negativePressed;
        PositivePressed = positivePressed;
    }

    /// <inheritdoc/>
    public static void Sample(Span<KeyboardAxis> instances, IInputSourceSampling sampling)
    {
        var count = instances.Length;
        if (count == 0) return;

        var keys = new Key[count * 2];
        for (var i = 0; i < count; i++)
        {
            keys[i * 2] = instances[i].Setting.Negative;
            keys[i * 2 + 1] = instances[i].Setting.Positive;
        }

        Span<bool> pressed = new bool[count * 2];
        sampling.Sample<Key, bool>(keys, pressed);

        for (var i = 0; i < count; i++)
            instances[i] = new KeyboardAxis(instances[i].Setting, pressed[i * 2], pressed[i * 2 + 1]);
    }

    /// <inheritdoc/>
    public static void Evaluate(ReadOnlySpan<KeyboardAxis> instances, Span<ActionResult<float>> results)
    {
        for (var i = 0; i < instances.Length; i++)
        {
            var instance = instances[i];
            results[i] = instance.PositivePressed
                ? ActionResult<float>.Value(1f)
                : instance.NegativePressed
                    ? ActionResult<float>.Value(-1f)
                    : ActionResult<float>.NoValue;
        }
    }
}
