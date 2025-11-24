using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Registry for state descriptors. States are registered with [StateRegistry.RegisterState("key")] attribute.
/// </summary>
[Registry(Identifier = "state")]
public partial class StateRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry
{
    /// <inheritdoc/>
    public static string Identifier => "state";

    /// <summary>
    /// Registers a state descriptor with the game state system.
    /// </summary>
    /// <typeparam name="TStateDescriptor">The state descriptor type to register.</typeparam>
    /// <param name="id">The state identification.</param>
    [RegistryMethod]
    public void RegisterState<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        gameStateManager.AddStateDescriptor<TStateDescriptor>(id);
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateDescriptor(id);
    }

}
