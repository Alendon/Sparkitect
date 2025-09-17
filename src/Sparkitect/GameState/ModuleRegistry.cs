using Sparkitect.Modding;

namespace Sparkitect.GameState;

[Registry(Identifier = "state_module")]
public partial class ModuleRegistry(IGameStateManager gameStateManager) : IRegistry
{
    [RegistryMethod]
    public void Register<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        gameStateManager.AddStateModule<TStateModule>(id);
    }
    
    
    
    public void PreRegister()
    {
    }

    public void PostRegister()
    {
    }

    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateModule(id);
    }
}
