using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Defines a state module - a reusable unit of functionality composed into states.
/// Modules contain state functions and declare dependencies on other modules.
/// </summary>
public interface IStateModule
{
    /// <summary>
    /// Gets the list of modules this module depends on. Dependencies must be present in the active state.
    /// </summary>
    public static abstract IReadOnlyList<Identification> RequiredModules { get; }

    /// <summary>
    /// Gets the unique identification for this module.
    /// </summary>
    public static abstract Identification Identification { get; }
}