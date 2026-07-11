using Silk.NET.Input;
using Sparkitect.Input;

namespace SpaceInvadersMod;

/// <summary>Named actions: one horizontal movement axis plus three digital keys.</summary>
public static class SpaceInvadersActions
{
    [ActionRegistry.RegisterAction("move_horizontal")]
    public static ActionDescription<float, InputAxis<Key>> MoveHorizontal() =>
        new(new InputAxis<Key>(Key.A, Key.D));

    [ActionRegistry.RegisterAction("shoot")]
    public static ActionDescription<bool, Key> Shoot() => new(Key.Space);

    [ActionRegistry.RegisterAction("toggle_pause")]
    public static ActionDescription<bool, Key> TogglePause() => new(Key.P);

    [ActionRegistry.RegisterAction("restart")]
    public static ActionDescription<bool, Key> Restart() => new(Key.R);
}
