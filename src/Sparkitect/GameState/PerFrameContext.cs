using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Context for per-frame stateless functions.
/// Contains state stack for module loaded checks.
/// </summary>
[PublicAPI]
public sealed class PerFrameContext
{
    /// <summary>
    /// The state stack from root to current. Each entry contains the state's modules and ID.
    /// </summary>
    public required IReadOnlyList<(IReadOnlyList<Identification> Modules, Identification StateId)> StateStack { get; init; }

    /// <summary>
    /// Modules declared by never-framed ancestor states (the Root anchor). Counted as loaded for the
    /// per-frame gate even though they appear in no pushed frame.
    /// </summary>
    public IReadOnlyList<Identification> AmbientModules { get; init; } = [];

    /// <summary>
    /// Checks if a module is loaded anywhere in the state stack or ambiently via a never-framed ancestor.
    /// </summary>
    public bool IsModuleLoaded(Identification moduleId) =>
        StateStack.Any(s => s.Modules.Contains(moduleId)) || AmbientModules.Contains(moduleId);
}