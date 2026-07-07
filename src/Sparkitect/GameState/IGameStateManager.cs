using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Manages game state transitions, the main loop, and mod loading/unloading.
/// </summary>
[PublicAPI]
[RegistryFacade<IGameStateManagerRegistryFacade>]
[StateFacade<IGameStateManagerStateFacade>]
public interface IGameStateManager
{
    /// <summary>
    /// Gets the current state's DI container.
    /// </summary>
    ICoreContainer CurrentCoreContainer { get; }

    /// <summary>
    /// Gets the IDs of currently loaded mods.
    /// </summary>
    IEnumerable<string> LoadedMods { get; }

    /// <summary>
    /// Active state stack, bottom-up: index 0 is the root state, the last entry is the
    /// current/deepest frame.
    /// </summary>
    IReadOnlyList<StateStackEntry> StateStack { get; }

    /// <summary>
    /// Checks if a mod with the given ID is currently loaded.
    /// </summary>
    /// <param name="modId">The mod identifier to check.</param>
    /// <returns>True if the mod is loaded, false otherwise.</returns>
    bool IsModLoaded(string modId);

    /// <summary>
    /// Requests a state transition to an immediate parent or child state.
    /// Transition executes between frames.
    /// </summary>
    /// <param name="stateId">The target state identification.</param>
    void Request(Identification stateId);

    /// <summary>
    /// Requests a state transition with additional mod loading. Target must be a child state.
    /// Loads specified mods, processes their registrations, then transitions.
    /// </summary>
    /// <param name="targetState">Lazy identification of the target state, resolved after mods are loaded.</param>
    /// <param name="additionalMods">Mod file identifiers (ID + Version) to load before transition.</param>
    void RequestWithModChange(ILazyIdentification targetState, IReadOnlyList<ModFileIdentifier> additionalMods);

    /// <summary>
    /// Requests engine shutdown.
    /// </summary>
    void Shutdown();
}

/// <summary>
/// Registry-exclusive facade for IGameStateManager. Accessible only within registry contexts.
/// </summary>
[FacadeFor<IGameStateManager>]
[PublicAPI]
public interface IGameStateManagerRegistryFacade
{
    /// <summary>
    /// Adds a state module to the registry system.
    /// </summary>
    /// <typeparam name="TStateModule">The module type.</typeparam>
    /// <param name="id">The module identification.</param>
    void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule, IHasIdentification, new();

    /// <summary>
    /// Removes a state module from the registry system.
    /// </summary>
    /// <param name="id">The module identification.</param>
    void RemoveStateModule(Identification id);

    /// <summary>
    /// Adds a game state to the registry system.
    /// </summary>
    /// <typeparam name="TGameState">The game state type.</typeparam>
    /// <param name="id">The state identification.</param>
    void AddStateDescriptor<TGameState>(Identification id) where TGameState : class, IGameState, IHasIdentification, new();

    /// <summary>
    /// Removes a state descriptor from the registry system.
    /// </summary>
    /// <param name="id">The state identification.</param>
    void RemoveStateDescriptor(Identification id);
}

/// <summary>
/// State-function-exclusive facade for IGameStateManager. Currently empty - reserved for future state-specific APIs.
/// </summary>
[FacadeFor<IGameStateManager>]
[PublicAPI]
public interface IGameStateManagerStateFacade
{

}
