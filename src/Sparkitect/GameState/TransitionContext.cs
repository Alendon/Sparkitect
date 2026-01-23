using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Context for transition stateless functions. Contains transition-specific data
/// used by scheduling implementations to filter and order functions.
/// </summary>
public sealed record TransitionContext
{
    /// <summary>
    /// The state stack from root to target. Each entry contains the state's modules and ID.
    /// Top element (StateStack[^1]) is the target state being entered/exited.
    /// </summary>
    public required IReadOnlyList<(IReadOnlyList<Identification> Modules, Identification StateId)> StateStack { get; init; }

    /// <summary>
    /// Whether this is an enter transition (OnCreate, OnFrameEnter) or exit (OnDestroy, OnFrameExit).
    /// </summary>
    public required bool IsEnterTransition { get; init; }

    /// <summary>
    /// The target state being entered or exited (top of stack).
    /// </summary>
    public Identification TargetStateId => StateStack[^1].StateId;

    /// <summary>
    /// The modules being added (enter) or removed (exit) - the delta at top of stack.
    /// </summary>
    public IReadOnlyList<Identification> DeltaModules => StateStack[^1].Modules;

    /// <summary>
    /// Checks if a module is loaded anywhere in the state stack.
    /// </summary>
    public bool IsModuleLoaded(Identification moduleId) =>
        StateStack.Any(s => s.Modules.Contains(moduleId));
}