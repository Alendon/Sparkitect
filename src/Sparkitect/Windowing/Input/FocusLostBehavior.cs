using JetBrains.Annotations;

namespace Sparkitect.Windowing.Input;

/// <summary>
/// Defines how input state is reported when a window loses focus.
/// </summary>
[PublicAPI]
public enum FocusLostBehavior
{
    /// <summary>
    /// All keys and buttons report as released when the window is unfocused.
    /// This is the default behavior.
    /// </summary>
    Released = 0,

    /// <summary>
    /// Input state is frozen at the last values when the window lost focus.
    /// </summary>
    Frozen = 1
}
