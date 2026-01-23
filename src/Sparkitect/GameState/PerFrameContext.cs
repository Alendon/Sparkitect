using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Context for per-frame stateless functions.
/// Contains state stack for module loaded checks.
/// </summary>
public sealed class PerFrameContext
{
    /// <summary>
    /// The state stack from root to current. Each entry contains the state's modules and ID.
    /// </summary>
    public required IReadOnlyList<(IReadOnlyList<Identification> Modules, Identification StateId)> StateStack { get; init; }

    /// <summary>
    /// Checks if a module is loaded anywhere in the state stack.
    /// </summary>
    public bool IsModuleLoaded(Identification moduleId) =>
        StateStack.Any(s => s.Modules.Contains(moduleId));
}