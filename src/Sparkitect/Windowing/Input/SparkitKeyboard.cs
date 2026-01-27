using Silk.NET.Input;
using SilkKeyboard = Silk.NET.Input.IKeyboard;

namespace Sparkitect.Windowing.Input;

internal class SparkitKeyboard : IKeyboard
{
    private readonly SilkKeyboard _silkKeyboard;
    private bool _windowFocused = true;
    private FocusLostBehavior _focusBehavior = FocusLostBehavior.Released;

    internal SparkitKeyboard(SilkKeyboard silkKeyboard)
    {
        _silkKeyboard = silkKeyboard;
    }

    public bool IsKeyDown(Key key)
    {
        if (!_windowFocused && _focusBehavior == FocusLostBehavior.Released)
            return false;

        return _silkKeyboard.IsKeyPressed(key);
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
