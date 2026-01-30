using JetBrains.Annotations;
using Sparkitect.DI.Container;
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
    /// Requests a state transition to an immediate parent or child state.
    /// Transition executes between frames.
    /// </summary>
    /// <param name="stateId">The target state identification.</param>
    /// <param name="payload">Optional payload data passed to the target state.</param>
    void Request(Identification stateId, object? payload = null);

    /// <summary>
    /// Requests a state transition with additional mod loading. Target must be a child state.
    /// Loads specified mods, processes their registrations, then transitions.
    /// </summary>
    /// <param name="stateIdFunc">Function returning the target state identification (called after mods are loaded).</param>
    /// <param name="additionalMods">Mod file identifiers (ID + Version) to load before transition.</param>
    /// <param name="payload">Optional payload data passed to the target state.</param>
    void RequestWithModChange(Func<Identification> stateIdFunc, IReadOnlyList<ModFileIdentifier> additionalMods, object? payload = null);

    /// <summary>
    /// Requests engine shutdown.
    /// </summary>
    void Shutdown();
}

/// <summary>
/// Registry-exclusive facade for IGameStateManager. Accessible only within registry contexts.
/// </summary>
public interface IGameStateManagerRegistryFacade
{
    /// <summary>
    /// Adds a state module to the registry system.
    /// </summary>
    /// <typeparam name="TStateModule">The module type.</typeparam>
    /// <param name="id">The module identification.</param>
    void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule;

    /// <summary>
    /// Removes a state module from the registry system.
    /// </summary>
    /// <param name="id">The module identification.</param>
    void RemoveStateModule(Identification id);

    /// <summary>
    /// Adds a state descriptor to the registry system.
    /// </summary>
    /// <typeparam name="TStateDescriptor">The state descriptor type.</typeparam>
    /// <param name="id">The state identification.</param>
    void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor;

    /// <summary>
    /// Removes a state descriptor from the registry system.
    /// </summary>
    /// <param name="id">The state identification.</param>
    void RemoveStateDescriptor(Identification id);
}

/// <summary>
/// State-function-exclusive facade for IGameStateManager. Currently empty - reserved for future state-specific APIs.
/// </summary>
public interface IGameStateManagerStateFacade
{

}
