using Sparkitect.Modding;

namespace Sparkitect.GameState;

[Registry(Identifier = "state_module")]
public partial class ModuleRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry
{
    [RegistryMethod]
    public void RegisterModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        gameStateManager.AddStateModule<TStateModule>(id);
    }

    public static string Identifier => "state_module";


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
