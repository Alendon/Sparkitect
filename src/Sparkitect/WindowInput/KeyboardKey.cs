using JetBrains.Annotations;
using Sparkitect.Input;
using Sparkitect.Input.Bindings;

namespace Sparkitect.WindowInput;

/// <summary>
/// A digital keyboard binding type (D-16): composes one key's sampled pressed-state into an
/// <see cref="ActionResult{T}"/> of <see cref="bool"/>. An unpressed key produces
/// <see cref="ActionResult{T}.NoValue"/>, NEVER <c>Value(false)</c> — "produces a value" means
/// "actively contributing" (D-19), which is what makes multi-key-per-action behave as OR under
/// first-match.
/// </summary>
/// <remarks>
/// <see cref="IsPressed"/> is the raw sampled channel slot for this instance's
/// <see cref="Setting"/>'s key, written by the owning <see cref="KeyboardSourceProvider"/>'s bulk
/// fill (via the InputManager's per-frame dirty-processing/re-point step, D-14/D-22) before
/// <see cref="Evaluate"/> runs — this type never samples the device itself.
/// </remarks>
[PublicAPI]
public readonly struct KeyboardKey : IBindingType<KeyboardKey, bool>
{
    /// <summary>The binding-backing setting: which key this instance is bound to.</summary>
    public KeyboardKeySetting Setting { get; }

    /// <summary>The current frame's sampled pressed-state for <see cref="Setting"/>'s key.</summary>
    public bool IsPressed { get; }

    /// <summary>Creates a keyboard-key binding instance.</summary>
    /// <param name="setting">The bound key.</param>
    /// <param name="isPressed">The current frame's sampled pressed-state (default unpressed).</param>
    public KeyboardKey(KeyboardKeySetting setting, bool isPressed = false)
    {
        Setting = setting;
        IsPressed = isPressed;
    }

    /// <inheritdoc/>
    public static void Evaluate(ReadOnlySpan<KeyboardKey> instances, Span<ActionResult<bool>> results)
    {
        for (var i = 0; i < instances.Length; i++)
            results[i] = instances[i].IsPressed ? ActionResult<bool>.Value(true) : ActionResult<bool>.NoValue;
    }
}
