using Silk.NET.Input;
using Sparkitect.WindowInput.Bindings;
using Sparkitect.Windowing.Input;

namespace Sparkitect.WindowInput;

/// <summary>
/// Bulk-fills keyboard key pressed-states for the keyboard channel (D-14): one
/// <see cref="Sample"/> call per frame over the deduped key list Input hands it, reading the
/// existing thin <see cref="SparkitKeyboard"/> wrapper. The natural home for
/// <see cref="FocusLostBehavior"/> (a device concern, D-14) — <see cref="SparkitKeyboard"/> lives
/// inside Windowing and is not itself part of the modding seam the bridge exposes.
/// </summary>
internal sealed class KeyboardSourceProvider : IInputSourceProvider<Key, bool>
{
    private readonly SparkitKeyboard _keyboard;

    internal KeyboardSourceProvider(SparkitKeyboard keyboard)
    {
        _keyboard = keyboard;
    }

    /// <summary>Configures how key state is reported when the owning window loses focus.</summary>
    internal FocusLostBehavior FocusBehavior
    {
        set => _keyboard.SetFocusBehavior(value);
    }

    /// <inheritdoc/>
    public void Sample(ReadOnlySpan<Key> values, Span<bool> results)
    {
        for (var i = 0; i < values.Length; i++)
            results[i] = _keyboard.IsKeyDown(values[i]);
    }
}
