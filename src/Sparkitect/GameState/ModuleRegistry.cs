using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Registry for state modules. Modules are registered with [ModuleRegistry.RegisterModule("key")] attribute.
/// </summary>
[Registry(Identifier = "state_module")]
public partial class ModuleRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry
{
    /// <summary>
    /// Registers a state module with the game state system.
    /// </summary>
    /// <typeparam name="TStateModule">The module type to register.</typeparam>
    /// <param name="id">The module identification.</param>
    [RegistryMethod]
    public void RegisterModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        gameStateManager.AddStateModule<TStateModule>(id);
    }

    /// <inheritdoc/>
    public static string Identifier => "state_module";

    /// <summary>
    /// Called before registration processing begins. Currently no pre-processing needed.
    /// </summary>
    public void PreRegister()
    {
    }

    /// <summary>
    /// Called after registration processing completes. Currently no post-processing needed.
    /// </summary>
    public void PostRegister()
    {
    }

    /// <inheritdoc/>
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateModule(id);
    }
}
