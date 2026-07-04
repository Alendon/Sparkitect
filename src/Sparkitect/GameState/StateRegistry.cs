using Sparkitect.Modding;

using JetBrains.Annotations;

namespace Sparkitect.GameState;

/// <summary>
/// Registry for state descriptors. States are registered with [StateRegistry.RegisterState("key")] attribute.
/// </summary>
[Registry(Identifier = "state")]
[PublicAPI]
public partial class StateRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry<CoreModule>
{
    /// <inheritdoc/>
    public static string Identifier => "state";

    /// <summary>
    /// Registers a game state with the game state system.
    /// </summary>
    /// <typeparam name="TGameState">The game state type to register.</typeparam>
    /// <param name="id">The state identification.</param>
    [RegistryMethod]
    public void RegisterState<TGameState>(Identification id) where TGameState : class, IGameState, IHasIdentification, new()
    {
        gameStateManager.AddStateDescriptor<TGameState>(id);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateDescriptor(id);
    }

}
