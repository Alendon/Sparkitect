using Sparkitect.Modding;

namespace Sparkitect.GameState;

[Registry(Identifier = "state")]
public partial class StateDescriptionRegistry(IGameStateManagerRegistryFacade gameStateManager) : IRegistry
{
 
    [RegistryMethod]
    public void RegisterStateAbc<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        gameStateManager.AddStateDescriptor<TStateDescriptor>(id);
    }
    
    
    
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateDescriptor(id);
    }
    
}
