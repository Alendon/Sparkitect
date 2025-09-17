using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[PublicAPI]
public interface IGameStateManager
{
    void Request(Identification stateId, object? payload = null);

    void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule;
    void RemoveStateModule(Identification id);
    void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor;
    void RemoveStateDescriptor(Identification id);
}

