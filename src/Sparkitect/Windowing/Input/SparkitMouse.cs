using System.Numerics;
using Silk.NET.Input;

namespace Sparkitect.Windowing.Input;

internal class SparkitMouse : IMouseInput
{
    private readonly IMouse _silkMouse;
    private Vector2 _lastPosition;
    private Vector2 _delta;
    private bool _windowFocused = true;
    private FocusLostBehavior _focusBehavior = FocusLostBehavior.Released;

    internal SparkitMouse(IMouse silkMouse)
    {
        _silkMouse = silkMouse;
        _lastPosition = _silkMouse.Position;
    }

    public Vector2 GetPosition()
    {
        return _silkMouse.Position;
    }

    public Vector2 GetDelta()
    {
        if (!_windowFocused && _focusBehavior == FocusLostBehavior.Released)
            return Vector2.Zero;

        return _delta;
    }

    public bool IsButtonDown(MouseButton button)
    {
        if (!_windowFocused && _focusBehavior == FocusLostBehavior.Released)
            return false;

        return _silkMouse.IsButtonPressed(button);
    }

    internal void UpdateDelta()
    {
        var currentPos = _silkMouse.Position;
        _delta = currentPos - _lastPosition;
        _lastPosition = currentPos;
    }

    internal void SetFocusState(bool focused)
    {
        _windowFocused = focused;
    }

    internal void SetFocusBehavior(FocusLostBehavior behavior)
    {
        _focusBehavior = behavior;
    }
}
