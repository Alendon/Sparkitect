using Sparkitect.Modding;

namespace Sparkitect.GameState;

[Registry(Identifier = "state")]
public partial class StateRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry
{
    public static string Identifier => "state";

    [RegistryMethod]
    public void RegisterState<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        gameStateManager.AddStateDescriptor<TStateDescriptor>(id);
    }
    
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateDescriptor(id);
    }
    
}
