using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Defines a state module - a reusable unit of functionality composed into states.
/// Modules contain state functions and declare dependencies on other modules.
/// </summary>
public interface IStateModule : IHasIdentification
{
    /// <summary>
    /// Gets the list of modules this module depends on. Dependencies must be present in the active state.
    /// </summary>
    public static abstract IReadOnlyList<Identification> RequiredModules { get; }
}