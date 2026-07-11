using JetBrains.Annotations;
using Silk.NET.Input;
using Sparkitect.Input;
using Sparkitect.WindowInput.Bindings;

namespace Sparkitect.WindowInput;

/// <summary>
/// A digital keyboard binding type: composes one key's sampled pressed-state into an
/// <see cref="ActionResult{T}"/> of <see cref="bool"/>. An unpressed key produces
/// <see cref="ActionResult{T}.NoValue"/>, NEVER <c>Value(false)</c> — "produces a value" means
/// "actively contributing", which is what makes multi-key-per-action behave as OR under
/// first-match.
/// </summary>
/// <remarks>
/// <see cref="IsPressed"/> is the raw sampled channel slot for <see cref="Setting"/>'s key,
/// written by <see cref="Sample"/> from the registered
/// <see cref="IInputSourceProvider{TValue,TRaw}"/> for the keyboard channel before
/// <see cref="Evaluate"/> runs — this type never samples the device itself.
/// </remarks>
[PublicAPI]
public readonly struct KeyboardKey : IBindingType<KeyboardKey, bool>
{
    /// <summary>The binding-backing setting: which key this instance is bound to.</summary>
    public Key Setting { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>.</summary>
    public bool IsPressed { get; }

    /// <summary>Creates a keyboard-key binding instance.</summary>
    /// <param name="setting">The bound key.</param>
    /// <param name="isPressed">The current frame's sampled pressed-state (default unpressed).</param>
    public KeyboardKey(Key setting, bool isPressed = false)
    {
        Setting = setting;
        IsPressed = isPressed;
    }

    /// <inheritdoc/>
    public static void Sample(Span<KeyboardKey> instances, IInputSourceSampling sampling)
    {
        var count = instances.Length;
        if (count == 0) return;

        var keys = new Key[count];
        for (var i = 0; i < count; i++) keys[i] = instances[i].Setting;

        Span<bool> pressed = new bool[count];
        sampling.Sample<Key, bool>(keys, pressed);

        for (var i = 0; i < count; i++)
            instances[i] = new KeyboardKey(instances[i].Setting, pressed[i]);
    }

    /// <inheritdoc/>
    public static void Evaluate(ReadOnlySpan<KeyboardKey> instances, Span<ActionResult<bool>> results)
    {
        for (var i = 0; i < instances.Length; i++)
            results[i] = instances[i].IsPressed ? ActionResult<bool>.Value(true) : ActionResult<bool>.NoValue;
    }
}
