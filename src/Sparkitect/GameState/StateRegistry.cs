using Sparkitect.GameState.Samples;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[Registry(Identifier = "state")]
public partial class StateRegistry(IGameStateManager gameStateManager) : IRegistry
{
 
    [RegistryMethod]
    public void RegisterState<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        gameStateManager.AddStateDescriptor<TStateDescriptor>(id);
    }
    
    
    
    
    public void Unregister(Identification id)
    {
        gameStateManager.RemoveStateDescriptor(id);
    }
    
    
    
    public void PreRegister()
    {
        
    }

    public void PostRegister()
    {
        
    }
}
