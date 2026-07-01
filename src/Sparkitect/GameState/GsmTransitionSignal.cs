namespace Sparkitect.GameState;

/// <summary>
/// Execution-time direction of the game-state manager while it runs a transition's enter or exit methods.
/// Consumed by the registry manager to auto-detect populate vs teardown; None outside a transition.
/// </summary>
internal enum GsmTransitionDirection
{
    None,
    Enter,
    Exit
}

/// <summary>
/// First-class transition-direction signal on the game-state manager. Valid during normal enter/exit
/// transitions and the root-entry bootstrap; None at all other times.
/// </summary>
internal interface IGameStateTransitionSignal
{
    GsmTransitionDirection TransitionDirection { get; }
}
