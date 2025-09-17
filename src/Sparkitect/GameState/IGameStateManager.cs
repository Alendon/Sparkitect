using JetBrains.Annotations;
using Sparkitect.GameState.Samples;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[PublicAPI]
public interface IGameStateManager
{
    /// <summary>
    /// Request a transition to a target state by id with an optional payload.
    /// </summary>
    void Request(Identification stateId, object? payload = null);

    void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule;
    void RemoveStateModule(Identification id);
    void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor;
    void RemoveStateDescriptor(Identification id);
}

