using System.Numerics;
using JetBrains.Annotations;
using Silk.NET.Input;
using Sparkitect.Input;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.WindowInput;

/// <summary>
/// A composite WASD-shaped keyboard binding type: composes four keys' sampled pressed-states into
/// an <see cref="ActionResult{T}"/> of <see cref="Vector2"/>. No key pressed produces
/// <see cref="ActionResult{T}.NoValue"/>, NEVER <c>Value(Vector2.Zero)</c> — "produces a value"
/// means "actively contributing"; "stick centered" and "nothing contributing" are different
/// facts. Backed by ONE composite value-type setting for all four keys.
/// </summary>
/// <remarks>
/// <c>Up</c>/<c>Down</c> drive Y toward +1/-1, <c>Left</c>/<c>Right</c> drive X toward -1/+1
/// (math-up-is-positive convention). Per-axis, both keys pressed simultaneously cancel to 0 on
/// that axis (unlike <see cref="KeyboardAxis"/>'s single-instance positive-wins tie-break — this
/// binding composes two independent axes from four keys, not one axis from two). The pressed-state
/// fields are raw sampled channel slots, written by <see cref="Sample"/> before
/// <see cref="Evaluate"/> runs — this type never samples the device itself.
/// </remarks>
[PublicAPI]
public readonly struct KeyboardVector2 : IBindingType<KeyboardVector2, Vector2>
{
    /// <summary>The binding-backing setting: which four keys drive this composite vector.</summary>
    public InputVector2<Key> Setting { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s up key.</summary>
    public bool UpPressed { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s down key.</summary>
    public bool DownPressed { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s left key.</summary>
    public bool LeftPressed { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s right key.</summary>
    public bool RightPressed { get; }

    /// <summary>Creates a keyboard-vector2 binding instance.</summary>
    /// <param name="setting">The four keys driving this composite vector.</param>
    /// <param name="upPressed">The current frame's sampled pressed-state for the up key.</param>
    /// <param name="downPressed">The current frame's sampled pressed-state for the down key.</param>
    /// <param name="leftPressed">The current frame's sampled pressed-state for the left key.</param>
    /// <param name="rightPressed">The current frame's sampled pressed-state for the right key.</param>
    public KeyboardVector2(
        InputVector2<Key> setting,
        bool upPressed = false,
        bool downPressed = false,
        bool leftPressed = false,
        bool rightPressed = false)
    {
        Setting = setting;
        UpPressed = upPressed;
        DownPressed = downPressed;
        LeftPressed = leftPressed;
        RightPressed = rightPressed;
    }

    /// <inheritdoc/>
    public static void Sample(Span<KeyboardVector2> instances, IInputSourceSampling sampling)
    {
        var count = instances.Length;
        if (count == 0) return;

        var keys = new Key[count * 4];
        for (var i = 0; i < count; i++)
        {
            var setting = instances[i].Setting;
            keys[i * 4] = setting.Up;
            keys[i * 4 + 1] = setting.Down;
            keys[i * 4 + 2] = setting.Left;
            keys[i * 4 + 3] = setting.Right;
        }

        Span<bool> pressed = new bool[count * 4];
        sampling.Sample<Key, bool>(keys, pressed);

        for (var i = 0; i < count; i++)
        {
            var b = i * 4;
            instances[i] = new KeyboardVector2(
                instances[i].Setting, pressed[b], pressed[b + 1], pressed[b + 2], pressed[b + 3]);
        }
    }

    /// <inheritdoc/>
    public static void Evaluate(ReadOnlySpan<KeyboardVector2> instances, Span<ActionResult<Vector2>> results)
    {
        for (var i = 0; i < instances.Length; i++)
        {
            var instance = instances[i];
            var anyPressed = instance.UpPressed || instance.DownPressed || instance.LeftPressed || instance.RightPressed;
            if (!anyPressed)
            {
                results[i] = ActionResult<Vector2>.NoValue;
                continue;
            }

            var x = (instance.RightPressed ? 1f : 0f) - (instance.LeftPressed ? 1f : 0f);
            var y = (instance.UpPressed ? 1f : 0f) - (instance.DownPressed ? 1f : 0f);
            results[i] = ActionResult<Vector2>.Value(new Vector2(x, y));
        }
    }
}
