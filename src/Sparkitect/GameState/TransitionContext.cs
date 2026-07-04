using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Context for transition stateless functions. Contains transition-specific data
/// used by scheduling implementations to filter and order functions.
/// </summary>
[PublicAPI]
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
    /// Modules declared by never-framed ancestor states (the Root anchor). Counted as loaded for
    /// frame-enter/exit/per-frame gates, but NOT part of <see cref="DeltaModules"/> — so their once-only
    /// create/destroy scheduling stays owned by the anchor and child transitions never re-run it.
    /// </summary>
    public IReadOnlyList<Identification> AmbientModules { get; init; } = [];

    /// <summary>
    /// Checks if a module is loaded anywhere in the state stack or ambiently via a never-framed ancestor.
    /// </summary>
    public bool IsModuleLoaded(Identification moduleId) =>
        StateStack.Any(s => s.Modules.Contains(moduleId)) || AmbientModules.Contains(moduleId);
}