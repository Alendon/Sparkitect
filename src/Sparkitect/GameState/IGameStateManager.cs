using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[PublicAPI]
public interface IGameStateManager
{
    
    ICoreContainer CurrentCoreContainer { get; }
    
    void Request(Identification stateId, object? payload = null);
}

public interface IGameStateManagerRegistryFacade
{
    void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule;
    void RemoveStateModule(Identification id);
    void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor;
    void RemoveStateDescriptor(Identification id);
}

