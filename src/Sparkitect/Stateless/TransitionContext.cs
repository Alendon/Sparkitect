using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Context for transition stateless functions. Contains transition-specific data
/// used by scheduling implementations to filter and order functions.
/// </summary>
public sealed class TransitionContext
{
    /// <summary>
    /// The state being entered or exited.
    /// </summary>
    public required Identification TargetStateId { get; init; }

    /// <summary>
    /// The modules active in the target state.
    /// </summary>
    public required IReadOnlyList<Identification> ActiveModuleIds { get; init; }

    /// <summary>
    /// Whether this is an enter transition (OnCreate, OnFrameEnter) or exit (OnDestroy, OnFrameExit).
    /// </summary>
    public required bool IsEnterTransition { get; init; }
}
