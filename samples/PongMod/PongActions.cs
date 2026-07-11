using Silk.NET.Input;
using Sparkitect.Input;

namespace PongMod;

/// <summary>Two named float axis actions, each with its default key pair.</summary>
public static class PongActions
{
    [ActionRegistry.RegisterAction("left_paddle")]
    public static ActionDescription<float, InputAxis<Key>> LeftPaddle() =>
        new(new InputAxis<Key>(Key.W, Key.S));

    [ActionRegistry.RegisterAction("right_paddle")]
    public static ActionDescription<float, InputAxis<Key>> RightPaddle() =>
        new(new InputAxis<Key>(Key.Up, Key.Down));
}
