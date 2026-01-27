using JetBrains.Annotations;
using Silk.NET.Input;

namespace Sparkitect.Windowing.Input;

/// <summary>
/// Abstraction for keyboard input state.
/// </summary>
[PublicAPI]
public interface IKeyboard
{
    /// <summary>
    /// Returns true if the specified key is currently held down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is pressed, false otherwise.</returns>
    bool IsKeyDown(Key key);
}
