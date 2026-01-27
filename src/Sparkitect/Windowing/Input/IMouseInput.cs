using System.Numerics;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Sparkitect.Windowing.Input;

/// <summary>
/// Abstraction for mouse input state.
/// Named IMouseInput to avoid collision with Silk.NET.Input.IMouse.
/// </summary>
/// <remarks>
/// Methods are used instead of properties to signal snapshot semantics -
/// values represent the state at the time of the call.
/// </remarks>
[PublicAPI]
public interface IMouseInput
{
    /// <summary>
    /// Returns the current mouse position in window coordinates.
    /// </summary>
    /// <returns>The mouse position as a Vector2.</returns>
    Vector2 GetPosition();

    /// <summary>
    /// Returns the mouse movement since the last frame.
    /// </summary>
    /// <returns>The delta movement as a Vector2.</returns>
    Vector2 GetDelta();

    /// <summary>
    /// Returns true if the specified mouse button is currently held down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is pressed, false otherwise.</returns>
    bool IsButtonDown(MouseButton button);
}
